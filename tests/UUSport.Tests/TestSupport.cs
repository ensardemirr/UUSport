using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Features.Audit;
using UUSport.Api.Features.Cash;
using UUSport.Api.Features.Imports;
using UUSport.Api.Infrastructure;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows")]

namespace UUSport.Tests;

public sealed class FakeUser(bool isAdmin, decimal maxDiscount) : ICurrentUser
{
    public Guid? UserId { get; } = Guid.CreateVersion7();
    public string? UserName => isAdmin ? "test-yonetici" : "test-muhasebe";
    public bool IsInRole(string role) => role == (isAdmin ? Roles.Yonetici : Roles.Muhasebe);
    public Task<decimal> GetMaxDiscountPercentAsync(CancellationToken ct) => Task.FromResult(maxDiscount);
}

/// <summary>
/// Testler gercek PostgreSQL uzerinde (uusport_test) ve UYGULAMA roluyle calisir; boylece RLS,
/// yetki ve kisitlar da sinanir. Her test kendi kiracisini olusturur ve RLS ile yalitilir.
/// </summary>
public sealed class TestWorld : IAsyncDisposable
{
    private static readonly DatabaseOptions Options = new() { Database = "uusport_test" };
    private static readonly Lazy<string> AppConnection = new(() => Options.BuildConnectionString(asOwner: false));

    public TenantContext Tenant { get; } = new();
    public ICurrentUser User { get; }
    public AppDbContext Db { get; }
    public AccountingService Accounting { get; }
    public AccountingQueries Queries { get; }
    public ImportService Imports { get; }
    public LoanService Loans { get; }
    public CheckService Checks { get; }

    private TestWorld(Guid tenantId, bool isAdmin, decimal maxDiscount)
    {
        Tenant.TenantId = tenantId;
        User = new FakeUser(isAdmin, maxDiscount);
        Db = CreateContext(Tenant);
        var audit = new AuditWriter(Db, Tenant, User, new HttpContextAccessor());
        Accounting = new AccountingService(Db, Tenant, User, audit);
        Queries = new AccountingQueries(Db);
        Imports = new ImportService(Db, Tenant, User, audit);
        Loans = new LoanService(Db, Tenant, User, audit, Accounting);
        Checks = new CheckService(Db, Tenant, User, audit, Accounting);
    }

    public static AppDbContext CreateContext(TenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(AppConnection.Value)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new TenantConnectionInterceptor(tenant))
            .Options);

    public static async Task<TestWorld> CreateAsync(bool isAdmin = false, decimal maxDiscount = 10)
    {
        var tenantId = Guid.CreateVersion7();
        await using (var db = CreateContext(new TenantContext()))
        {
            db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test " + tenantId.ToString("N")[..8] });
            await db.SaveChangesAsync();
        }
        return new TestWorld(tenantId, isAdmin, maxDiscount);
    }

    /// <summary>Ayni kiracida baska bir kullanici (ornegin yonetici) olarak islem yapmak icin.</summary>
    public TestWorld As(bool isAdmin, decimal maxDiscount = 10) => new(Tenant.Require(), isAdmin, maxDiscount);

    public async Task<Party> PartyAsync(string name, PartyKind kind = PartyKind.Customer)
    {
        var p = new Party
        {
            TenantId = Tenant.Require(), Code = "T" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Name = name, Kind = kind, SearchKey = TextRules.SearchKey(name),
        };
        Db.Parties.Add(p);
        await Db.SaveChangesAsync();
        return p;
    }

    public async Task<Item> ItemAsync(string name, decimal price, ItemKind kind = ItemKind.Stock, decimal vat = 20, bool incl = true)
    {
        var i = new Item
        {
            TenantId = Tenant.Require(), Code = "I" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Name = name, Kind = kind, UnitPrice = price, VatRate = vat, PriceIncludesVat = incl, SearchKey = TextRules.SearchKey(name),
        };
        Db.Items.Add(i);
        await Db.SaveChangesAsync();
        return i;
    }

    public async Task<CashAccount> AccountAsync(CashAccountKind kind = CashAccountKind.Bank)
    {
        var a = new CashAccount { TenantId = Tenant.Require(), Name = kind.ToString(), Kind = kind };
        Db.CashAccounts.Add(a);
        await Db.SaveChangesAsync();
        return a;
    }

    /// <summary>Stok girisi icin tedarikciden alis yapar.</summary>
    public async Task StockInAsync(Item item, decimal qty)
    {
        var supplier = await PartyAsync("Test Tedarikçi", PartyKind.Supplier);
        await Accounting.CreateTradeLineAsync(new TradeLineInput(supplier.Id, item.Id, TradeDirection.Purchase, null, qty, 1m, 0, null), default);
    }

    public Task<decimal> BalanceAsync(Guid partyId) =>
        Db.LedgerEntries.Where(e => e.PartyId == partyId).SumAsync(e => e.Debit - e.Credit);

    public ValueTask DisposeAsync() => Db.DisposeAsync();
}
