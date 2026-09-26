using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Audit;
using UUSport.Api.Features.Auth;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Catalog;

public sealed record ItemInput(string Name, ItemKind Kind, string? Code, string? Unit, decimal UnitPrice,
    bool PriceIncludesVat, decimal VatRate, bool IsActive = true);

public sealed record CashAccountInput(string Name, CashAccountKind Kind, bool IsActive = true);

public static class CatalogEndpoints
{
    public static async Task<string> NextItemCodeAsync(AppDbContext db, CancellationToken ct)
    {
        var codes = await db.Items.Where(i => i.Code.StartsWith("U")).Select(i => i.Code).ToListAsync(ct);
        var max = codes.Select(c => int.TryParse(c.AsSpan(1), out var n) ? n : 0).DefaultIfEmpty(0).Max();
        return $"U{max + 1:000000}";
    }

    public static void ApplyItem(Item item, ItemInput input)
    {
        item.Name = TextRules.CleanName(input.Name);
        if (item.Name.Length == 0) throw new DomainException("name", "Ürün/hizmet adı zorunlu.");
        if (input.UnitPrice < 0) throw new DomainException("price", "Birim fiyat eksi olamaz.");
        if (input.VatRate is < 0 or > 100) throw new DomainException("vat_rate", "KDV oranı 0 ile 100 arasında olmalı.");
        item.Kind = input.Kind;
        item.Unit = string.IsNullOrWhiteSpace(input.Unit) ? "adet" : input.Unit.Trim();
        item.UnitPrice = input.UnitPrice;
        item.PriceIncludesVat = input.PriceIncludesVat;
        item.VatRate = input.VatRate;
        item.IsActive = input.IsActive;
        item.SearchKey = TextRules.SearchKey(item.Name, item.Code);
    }

    public static void MapCatalog(this IEndpointRouteBuilder app)
    {
        var items = app.MapGroup("/api/items").RequireAuthorization(Policies.Staff);

        items.MapGet("", async (string? q, bool? includeInactive, AppDbContext db, CancellationToken ct) =>
        {
            var query = db.Items.AsQueryable();
            if (includeInactive != true) query = query.Where(i => i.IsActive);
            if (!string.IsNullOrWhiteSpace(q))
            {
                var key = TextRules.SearchKey(q);
                query = query.Where(i => i.SearchKey.Contains(key));
            }
            var list = await query.OrderBy(i => i.Name).Take(500).ToListAsync(ct);
            var ids = list.Select(i => i.Id).ToList();
            var stock = await db.StockMovements.Where(m => ids.Contains(m.ItemId))
                .GroupBy(m => m.ItemId).Select(x => new { x.Key, Qty = x.Sum(m => m.Quantity) })
                .ToDictionaryAsync(x => x.Key, x => x.Qty, ct);
            return Results.Ok(list.Select(i => new
            {
                i.Id, i.Code, i.Name, kind = AppDbContext.ToSnake(i.Kind.ToString()), i.Unit, i.UnitPrice,
                i.PriceIncludesVat, i.VatRate, i.IsActive,
                stockOnHand = i.Kind == ItemKind.Stock ? stock.GetValueOrDefault(i.Id) : (decimal?)null,
            }));
        });

        items.MapPost("", async (ItemInput input, AppDbContext db, TenantContext tenant, ICurrentUser user,
            AuditWriter audit, CancellationToken ct) =>
        {
            var item = new Item { TenantId = tenant.Require(), CreatedByUserId = user.UserId };
            item.Code = string.IsNullOrWhiteSpace(input.Code) ? await NextItemCodeAsync(db, ct) : input.Code.Trim().ToUpperInvariant();
            ApplyItem(item, input);
            db.Items.Add(item);
            audit.Add("item.create", "item", item.Id, new { item.Code, item.UnitPrice, item.VatRate });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { item.Id, item.Code });
        });

        items.MapPut("/{id:guid}", async (Guid id, ItemInput input, AppDbContext db, AuditWriter audit, CancellationToken ct) =>
        {
            var item = await db.Items.SingleOrDefaultAsync(i => i.Id == id, ct) ?? throw new NotFoundException("Ürün/hizmet");
            var hasMovements = await db.TradeLines.AnyAsync(l => l.ItemId == id, ct);
            if (hasMovements && item.Kind != input.Kind)
                throw new DomainException("kind", "Hareket görmüş bir kalemin stok/hizmet türü değiştirilemez.");
            var oldPrice = item.UnitPrice;
            if (!string.IsNullOrWhiteSpace(input.Code)) item.Code = input.Code.Trim().ToUpperInvariant();
            ApplyItem(item, input);
            audit.Add("item.update", "item", item.Id, new { oldPrice, item.UnitPrice, item.VatRate, item.IsActive });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        });

        var cash = app.MapGroup("/api/cash-accounts").RequireAuthorization(Policies.Staff);

        cash.MapGet("", async (AppDbContext db, CancellationToken ct) =>
        {
            var accounts = await db.CashAccounts.OrderBy(a => a.Kind).ThenBy(a => a.Name).ToListAsync(ct);
            var balances = await (from p in db.Payments
                                  group p by p.CashAccountId into g
                                  select new
                                  {
                                      g.Key,
                                      In = g.Where(p => (p.Direction == PaymentDirection.Receipt) == (p.ReversesPaymentId == null)).Sum(p => p.Amount),
                                      Out = g.Where(p => (p.Direction == PaymentDirection.Receipt) != (p.ReversesPaymentId == null)).Sum(p => p.Amount),
                                  }).ToDictionaryAsync(x => x.Key, x => x.In - x.Out, ct);
            return Results.Ok(accounts.Select(a => new
            {
                a.Id, a.Name, kind = AppDbContext.ToSnake(a.Kind.ToString()), a.IsActive, balance = balances.GetValueOrDefault(a.Id),
            }));
        });

        cash.MapPost("", async (CashAccountInput input, AppDbContext db, TenantContext tenant, AuditWriter audit, CancellationToken ct) =>
        {
            var name = TextRules.CleanName(input.Name);
            if (name.Length == 0) throw new DomainException("name", "Hesap adı zorunlu.");
            var a = new CashAccount { TenantId = tenant.Require(), Name = name, Kind = input.Kind, IsActive = input.IsActive };
            db.CashAccounts.Add(a);
            audit.Add("cash_account.create", "cash_account", a.Id, new { a.Name, kind = a.Kind.ToString() });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { a.Id });
        }).RequireAuthorization(Policies.Admin);

        cash.MapPut("/{id:guid}", async (Guid id, CashAccountInput input, AppDbContext db, AuditWriter audit, CancellationToken ct) =>
        {
            var a = await db.CashAccounts.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new NotFoundException("Hesap");
            a.Name = TextRules.CleanName(input.Name);
            a.IsActive = input.IsActive;
            audit.Add("cash_account.update", "cash_account", a.Id, new { a.Name, a.IsActive });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { ok = true });
        }).RequireAuthorization(Policies.Admin);
    }
}
