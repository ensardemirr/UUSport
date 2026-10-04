using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Audit;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Accounting;

public sealed record TradeLineInput(
    Guid PartyId, Guid ItemId, TradeDirection Direction, DateOnly? OccurredOn,
    decimal Quantity, decimal? UnitPrice, decimal DiscountPercent, string? Description);

public sealed record ReverseLineInput(decimal Quantity, string Reason, DateOnly? OccurredOn);

public sealed record AllocationInput(Guid TradeLineId, decimal Amount);

public sealed record PaymentInput(
    Guid PartyId, PaymentDirection Direction, PaymentMethod Method, Guid CashAccountId, decimal Amount,
    DateOnly? OccurredOn, string? Reference, string? Description, List<AllocationInput>? Allocations);

public sealed record DocumentVatInput(decimal VatRate, decimal NetAmount, decimal VatAmount);

public sealed record DocumentLinkInput(Guid TradeLineId, decimal Quantity);

public sealed record DocumentInput(
    Guid PartyId, TradeDirection Direction, DocumentType Type, string DocumentNo, DateOnly DocumentDate,
    decimal GrossTotal, List<DocumentVatInput> VatLines, List<DocumentLinkInput> Links, string? Note);

public sealed record DocumentResult(
    Guid DocumentId, decimal EnteredGross, decimal SystemNet, decimal SystemVat, decimal SystemGross, decimal Difference);

/// <summary>Satir kilidi alinabilen tablolar (beyaz liste; SQL'e yalniz bunlar girer).</summary>
public enum LockTable { Payment, Document, TradeLine, Item, OpeningBalance, LoanInstallment, CheckNote }

/// <summary>
/// ADR-004 cari hareket modeli. Cariye yalniz satir, tahsilat/odeme ve onayli acilis bakiyesi
/// yazar; belge ve dagitim cariye tutar yazmaz. Her komut tek transaction'da calisir; sinir
/// kontrolu yapilan satirlar okunmadan once kilitlenir (S-02) ve denetim kaydi ayni islemde yazilir.
/// </summary>
public sealed class AccountingService(AppDbContext db, TenantContext tenant, ICurrentUser user, AuditWriter audit)
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    // =====================================================================
    // Transaction ve kilit (S-02)
    // =====================================================================

    /// <summary>
    /// Komutu tek transaction'da calistirir. Ic ice cagrida (ör. kredi taksiti → tahsilat/odeme) disaridaki
    /// transaction kullanilir; herhangi bir adimda hata olursa hicbir finansal etki kalmaz.
    /// </summary>
    public async Task<T> InTransactionAsync<T>(Func<Task<T>> work, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is not null) return await work();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var result = await work();
        await tx.CommitAsync(ct);
        return result;
    }

    public Task InTransactionAsync(Func<Task> work, CancellationToken ct) =>
        InTransactionAsync(async () => { await work(); return 0; }, ct);

    /// <summary>
    /// Okuma-kontrol-yazma yarisini onlemek icin satirlari FOR UPDATE ile kilitler; kilitten SONRA okunan
    /// deger guncel olur. Kilitlenmeyi onlemek icin sira: payment → document → trade_line → item → digerleri.
    /// </summary>
    internal async Task LockAsync(LockTable table, IEnumerable<Guid> ids, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Satır kilidi yalnız transaction içinde alınır.");
        var ordered = ids.Distinct().Order().ToArray();
        if (ordered.Length == 0) return;
        var sql = table switch
        {
            LockTable.Payment => "SELECT 1 FROM finance.payment WHERE id = ANY({0}) ORDER BY id FOR UPDATE",
            LockTable.Document => "SELECT 1 FROM billing.document WHERE id = ANY({0}) ORDER BY id FOR UPDATE",
            LockTable.TradeLine => "SELECT 1 FROM billing.trade_line WHERE id = ANY({0}) ORDER BY id FOR UPDATE",
            LockTable.Item => "SELECT 1 FROM inventory.item WHERE id = ANY({0}) ORDER BY id FOR UPDATE",
            LockTable.OpeningBalance => "SELECT 1 FROM finance.opening_balance WHERE id = ANY({0}) ORDER BY id FOR UPDATE",
            LockTable.LoanInstallment => "SELECT 1 FROM finance.loan_installment WHERE id = ANY({0}) ORDER BY id FOR UPDATE",
            LockTable.CheckNote => "SELECT 1 FROM finance.check_note WHERE id = ANY({0}) ORDER BY id FOR UPDATE",
            _ => throw new ArgumentOutOfRangeException(nameof(table)),
        };
        await db.Database.ExecuteSqlRawAsync(sql, [ordered], ct);
    }

    // =====================================================================
    // Satis / alis satiri
    // =====================================================================

    public Task<TradeLine> CreateTradeLineAsync(TradeLineInput input, CancellationToken ct) =>
        InTransactionAsync(() => CreateTradeLineCoreAsync(input, ct), ct);

    private async Task<TradeLine> CreateTradeLineCoreAsync(TradeLineInput input, CancellationToken ct)
    {
        var tenantId = tenant.Require();
        if (input.Quantity <= 0) throw new DomainException("quantity", "Miktar sıfırdan büyük olmalı.");
        if (Money.RoundQuantity(input.Quantity) != input.Quantity)
            throw new DomainException("quantity", "Miktar en fazla 4 ondalık basamak olabilir.");
        if (input.DiscountPercent is < 0 or > 100)
            throw new DomainException("discount", "İskonto yüzdesi 0 ile 100 arasında olmalı.");

        var party = await RequireActiveParty(input.PartyId, ct);
        EnsurePartyAllows(party, input.Direction);
        var item = await db.Items.SingleOrDefaultAsync(i => i.Id == input.ItemId, ct)
                   ?? throw new NotFoundException("Ürün/hizmet");
        if (!item.IsActive) throw new DomainException("item_inactive", "Ürün/hizmet kullanım dışı.");

        if (input.Direction == TradeDirection.Sale)
        {
            var max = await user.GetMaxDiscountPercentAsync(ct);
            if (input.DiscountPercent > max)
                throw new DomainException("discount_limit",
                    $"İskonto yetki sınırını aşıyor. Bu kullanıcı en fazla %{max:0.##} iskonto uygulayabilir.");
        }

        var unitPrice = input.UnitPrice ?? item.UnitPrice;
        if (unitPrice < 0) throw new DomainException("price", "Birim fiyat eksi olamaz.");
        var amounts = Money.PriceLine(input.Quantity, unitPrice, item.PriceIncludesVat, input.DiscountPercent, item.VatRate);
        if (amounts.Gross <= 0)
            throw new DomainException("zero_amount", "Tutarı sıfır olan satır kaydedilemez.");

        var on = input.OccurredOn ?? Today;
        var line = new TradeLine
        {
            TenantId = tenantId,
            CreatedByUserId = user.UserId,
            PartyId = party.Id,
            ItemId = item.Id,
            Direction = input.Direction,
            OccurredOn = on,
            Quantity = input.Quantity,
            UnitPrice = unitPrice,
            PriceIncludesVat = item.PriceIncludesVat,
            DiscountPercent = input.DiscountPercent,
            VatRate = item.VatRate,
            NetAmount = amounts.Net,
            VatAmount = amounts.Vat,
            GrossAmount = amounts.Gross,
            Description = Clean(input.Description),
        };
        db.TradeLines.Add(line);

        if (item.Kind == ItemKind.Stock)
        {
            var delta = input.Direction == TradeDirection.Sale ? -input.Quantity : input.Quantity;
            await AddStockMovement(item, delta, line.Id, on, ct);
        }

        var isSale = input.Direction == TradeDirection.Sale;
        AddLedger(party.Id, on, isSale ? LedgerSources.SalesLine : LedgerSources.PurchaseLine, line.Id,
            debit: isSale ? amounts.Gross : 0, credit: isSale ? 0 : amounts.Gross,
            description: $"{(isSale ? "Satış" : "Alış")}: {item.Name} × {input.Quantity:0.####}");

        audit.Add(isSale ? "sales_line.create" : "purchase_line.create", "trade_line", line.Id, new
        {
            line.PartyId, line.ItemId, line.Quantity, line.UnitPrice, catalogPrice = item.UnitPrice,
            line.DiscountPercent, line.GrossAmount,
        });
        await db.SaveChangesAsync(ct);
        return line;
    }

    /// <summary>ADR-004 S4: iade/duzeltme ters satirla yapilir; ilk satir degismez.</summary>
    public Task<TradeLine> ReverseTradeLineAsync(Guid lineId, ReverseLineInput input, CancellationToken ct) =>
        InTransactionAsync(() => ReverseTradeLineCoreAsync(lineId, input, ct), ct);

    private async Task<TradeLine> ReverseTradeLineCoreAsync(Guid lineId, ReverseLineInput input, CancellationToken ct)
    {
        var tenantId = tenant.Require();
        var reason = Clean(input.Reason) ?? throw new DomainException("reason", "İade/düzeltme gerekçesi zorunlu.");
        await LockAsync(LockTable.TradeLine, [lineId], ct);
        var original = await db.TradeLines.SingleOrDefaultAsync(l => l.Id == lineId, ct)
                       ?? throw new NotFoundException("Satır");
        if (original.ReversesLineId is not null)
            throw new DomainException("reverse_of_reverse", "Ters kaydın tersi alınamaz.");

        var previous = await db.TradeLines.Where(l => l.ReversesLineId == original.Id).ToListAsync(ct);
        var reversedQty = -previous.Sum(l => l.Quantity);
        var remaining = original.Quantity - reversedQty;
        if (input.Quantity <= 0 || input.Quantity > remaining)
            throw new DomainException("quantity", $"İade miktarı 0 ile {remaining:0.####} arasında olmalı.");

        LineAmounts amounts;
        if (input.Quantity == remaining)
        {
            // Son iade: toplam tam olarak sifirlanir, kurus farki kalmaz.
            amounts = new LineAmounts(
                -(original.NetAmount + previous.Sum(l => l.NetAmount)),
                -(original.VatAmount + previous.Sum(l => l.VatAmount)),
                -(original.GrossAmount + previous.Sum(l => l.GrossAmount)));
        }
        else
        {
            amounts = Money.PriceLine(-input.Quantity, original.UnitPrice, original.PriceIncludesVat,
                original.DiscountPercent, original.VatRate);
        }

        var item = await db.Items.SingleAsync(i => i.Id == original.ItemId, ct);
        var on = input.OccurredOn ?? Today;
        var reversal = new TradeLine
        {
            TenantId = tenantId,
            CreatedByUserId = user.UserId,
            PartyId = original.PartyId,
            ItemId = original.ItemId,
            Direction = original.Direction,
            OccurredOn = on,
            Quantity = -input.Quantity,
            UnitPrice = original.UnitPrice,
            PriceIncludesVat = original.PriceIncludesVat,
            DiscountPercent = original.DiscountPercent,
            VatRate = original.VatRate,
            NetAmount = amounts.Net,
            VatAmount = amounts.Vat,
            GrossAmount = amounts.Gross,
            ReversesLineId = original.Id,
            Description = reason,
        };
        db.TradeLines.Add(reversal);

        if (item.Kind == ItemKind.Stock)
        {
            var delta = original.Direction == TradeDirection.Sale ? input.Quantity : -input.Quantity;
            await AddStockMovement(item, delta, reversal.Id, on, ct);
        }

        var isSale = original.Direction == TradeDirection.Sale;
        var abs = -amounts.Gross;
        AddLedger(original.PartyId, on, isSale ? LedgerSources.SalesLineReversal : LedgerSources.PurchaseLineReversal,
            reversal.Id, debit: isSale ? 0 : abs, credit: isSale ? abs : 0,
            description: $"İade: {item.Name} × {input.Quantity:0.####} — {reason}");

        var released = await ReleaseExcessAllocationsAsync(original, original.GrossAmount + previous.Sum(l => l.GrossAmount) + amounts.Gross, ct);

        audit.Add("trade_line.reverse", "trade_line", reversal.Id,
            new { originalLineId = original.Id, quantity = input.Quantity, reason, gross = amounts.Gross, releasedAllocation = released });
        await db.SaveChangesAsync(ct);
        return reversal;
    }

    /// <summary>
    /// Iade sonrasi satirin net tutari, ona dagitilmis tahsilattan kucukse fazlasi dagitimdan cikarilir
    /// (en son dagitimdan baslayarak, eksi tutarli dagitim satiriyla). Fazla tutar tahsilatta
    /// "dagitilmamis" (avans) olarak kalir; cari bakiyesi zaten dogrudur (X:ACC-07).
    /// </summary>
    private async Task<decimal> ReleaseExcessAllocationsAsync(TradeLine original, decimal netGrossAfter, CancellationToken ct)
    {
        var allocations = await db.PaymentAllocations.Where(a => a.TradeLineId == original.Id).ToListAsync(ct);
        var excess = allocations.Sum(a => a.Amount) - netGrossAfter;
        if (excess <= 0) return 0;

        var released = excess;
        var byPayment = allocations.GroupBy(a => a.PaymentId)
            .Select(g => new { PaymentId = g.Key, Net = g.Sum(a => a.Amount), Last = g.Max(a => a.CreatedAt) })
            .Where(x => x.Net > 0).OrderByDescending(x => x.Last).ToList();
        foreach (var p in byPayment)
        {
            if (excess <= 0) break;
            var take = Math.Min(p.Net, excess);
            db.PaymentAllocations.Add(new PaymentAllocation
            {
                TenantId = original.TenantId, CreatedByUserId = user.UserId, PaymentId = p.PaymentId,
                TradeLineId = original.Id, Amount = -take,
            });
            excess -= take;
        }
        return released;
    }

    // =====================================================================
    // Tahsilat / odeme ve dagitim
    // =====================================================================

    public Task<Payment> CreatePaymentAsync(PaymentInput input, CancellationToken ct) =>
        InTransactionAsync(() => CreatePaymentCoreAsync(input, ct), ct);

    private async Task<Payment> CreatePaymentCoreAsync(PaymentInput input, CancellationToken ct)
    {
        var tenantId = tenant.Require();
        if (input.Amount <= 0 || !Money.HasAtMostTwoDecimals(input.Amount))
            throw new DomainException("amount", "Tutar sıfırdan büyük ve en fazla 2 ondalık basamaklı olmalı.");
        var party = await RequireActiveParty(input.PartyId, ct);
        EnsurePartyAllows(party, input.Direction == PaymentDirection.Receipt ? TradeDirection.Sale : TradeDirection.Purchase);

        var account = await db.CashAccounts.SingleOrDefaultAsync(a => a.Id == input.CashAccountId, ct)
                      ?? throw new NotFoundException("Kasa/banka hesabı");
        if (!account.IsActive) throw new DomainException("account_inactive", "Kasa/banka hesabı kullanım dışı.");
        EnsureMethodMatchesAccount(input.Direction, input.Method, account.Kind);

        var on = input.OccurredOn ?? Today;
        var payment = new Payment
        {
            TenantId = tenantId,
            CreatedByUserId = user.UserId,
            PartyId = party.Id,
            Direction = input.Direction,
            Method = input.Method,
            CashAccountId = account.Id,
            Amount = input.Amount,
            OccurredOn = on,
            Reference = Clean(input.Reference),
            Description = Clean(input.Description),
        };
        db.Payments.Add(payment);

        var isReceipt = input.Direction == PaymentDirection.Receipt;
        AddLedger(party.Id, on, LedgerSources.Payment, payment.Id,
            debit: isReceipt ? 0 : input.Amount, credit: isReceipt ? input.Amount : 0,
            description: $"{(isReceipt ? "Tahsilat" : "Ödeme")} — {MethodName(input.Method)} / {account.Name}");

        if (input.Allocations is { Count: > 0 })
            await AllocateInternal(payment, input.Allocations, ct);

        audit.Add(isReceipt ? "payment.receipt" : "payment.disbursement", "payment", payment.Id,
            new { payment.PartyId, payment.Amount, method = payment.Method.ToString(), account = account.Name,
                  allocations = input.Allocations?.Count ?? 0 });
        await db.SaveChangesAsync(ct);
        return payment;
    }

    /// <summary>ADR-004 S8: dagitim yeni cari hareket uretmez.</summary>
    public Task AllocateAsync(Guid paymentId, List<AllocationInput> allocations, CancellationToken ct) =>
        InTransactionAsync(async () =>
        {
            await LockAsync(LockTable.Payment, [paymentId], ct);
            var payment = await db.Payments.SingleOrDefaultAsync(p => p.Id == paymentId, ct)
                          ?? throw new NotFoundException("Tahsilat/ödeme");
            await AllocateInternal(payment, allocations, ct);
            audit.Add("payment.allocate", "payment", payment.Id, new { count = allocations.Count, total = allocations.Sum(a => a.Amount) });
            await db.SaveChangesAsync(ct);
        }, ct);

    private async Task AllocateInternal(Payment payment, List<AllocationInput> allocations, CancellationToken ct)
    {
        if (payment.ReversesPaymentId is not null)
            throw new DomainException("allocation", "Ters kayıt dağıtılamaz.");
        if (await db.Payments.AnyAsync(p => p.ReversesPaymentId == payment.Id, ct))
            throw new DomainException("allocation", "İptal edilmiş tahsilat/ödeme dağıtılamaz.");
        if (allocations.Select(a => a.TradeLineId).Distinct().Count() != allocations.Count)
            throw new DomainException("allocation", "Aynı satır bir dağıtımda iki kez seçilemez.");

        var ids = allocations.Select(a => a.TradeLineId).ToList();
        await LockAsync(LockTable.TradeLine, ids, ct);

        var expectedDirection = payment.Direction == PaymentDirection.Receipt ? TradeDirection.Sale : TradeDirection.Purchase;
        var already = await db.PaymentAllocations.Where(a => a.PaymentId == payment.Id).SumAsync(a => (decimal?)a.Amount, ct) ?? 0;
        var paymentRemaining = payment.Amount - already;

        var lines = await db.TradeLines.Where(l => ids.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);
        var allocatedByLine = await db.PaymentAllocations.Where(a => ids.Contains(a.TradeLineId))
            .GroupBy(a => a.TradeLineId).Select(g => new { g.Key, Sum = g.Sum(a => a.Amount) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, ct);
        // Iade satirlarinin (eksi) brutu dusulur: acik tutar net ekonomik tutar uzerinden (X:ACC-07).
        var reversedGrossByLine = await db.TradeLines.Where(l => l.ReversesLineId != null && ids.Contains(l.ReversesLineId!.Value))
            .GroupBy(l => l.ReversesLineId!.Value).Select(g => new { g.Key, Sum = g.Sum(l => l.GrossAmount) })
            .ToDictionaryAsync(x => x.Key, x => x.Sum, ct);

        foreach (var a in allocations)
        {
            if (a.Amount <= 0 || !Money.HasAtMostTwoDecimals(a.Amount))
                throw new DomainException("allocation", "Dağıtım tutarı sıfırdan büyük ve en fazla 2 ondalık basamaklı olmalı.");
            if (!lines.TryGetValue(a.TradeLineId, out var line)) throw new NotFoundException("Satır");
            if (line.PartyId != payment.PartyId)
                throw new DomainException("allocation", "Tahsilat yalnız aynı carinin satırlarına dağıtılabilir.");
            if (line.Direction != expectedDirection || line.ReversesLineId is not null)
                throw new DomainException("allocation", "Bu satıra dağıtım yapılamaz.");
            var open = line.GrossAmount + reversedGrossByLine.GetValueOrDefault(line.Id) - allocatedByLine.GetValueOrDefault(line.Id);
            if (a.Amount > open)
                throw new DomainException("allocation", $"Dağıtım satırın açık tutarını ({open:N2} TL) aşıyor.");
            if (a.Amount > paymentRemaining)
                throw new DomainException("allocation", $"Dağıtım toplamı kalan tutarı ({paymentRemaining:N2} TL) aşıyor.");
            paymentRemaining -= a.Amount;

            db.PaymentAllocations.Add(new PaymentAllocation
            {
                TenantId = payment.TenantId,
                CreatedByUserId = user.UserId,
                PaymentId = payment.Id,
                TradeLineId = line.Id,
                Amount = a.Amount,
            });
        }
    }

    /// <summary>ADR-004 S9: yanlis dagitim silinmez, ters dagitimla kapatilir.</summary>
    public Task ReverseAllocationAsync(Guid allocationId, CancellationToken ct) =>
        InTransactionAsync(() => ReverseAllocationCoreAsync(allocationId, ct), ct);

    private async Task ReverseAllocationCoreAsync(Guid allocationId, CancellationToken ct)
    {
        var paymentId = await db.PaymentAllocations.Where(a => a.Id == allocationId).Select(a => (Guid?)a.PaymentId).SingleOrDefaultAsync(ct)
                        ?? throw new NotFoundException("Dağıtım");
        await LockAsync(LockTable.Payment, [paymentId], ct);
        var alloc = await db.PaymentAllocations.SingleAsync(a => a.Id == allocationId, ct);
        if (alloc.Amount < 0 || alloc.ReversesAllocationId is not null)
            throw new DomainException("allocation", "Ters dağıtımın tersi alınamaz.");
        if (await db.PaymentAllocations.AnyAsync(a => a.ReversesAllocationId == alloc.Id, ct))
            throw new DomainException("allocation", "Bu dağıtım zaten geri alınmış.");
        db.PaymentAllocations.Add(new PaymentAllocation
        {
            TenantId = alloc.TenantId,
            CreatedByUserId = user.UserId,
            PaymentId = alloc.PaymentId,
            TradeLineId = alloc.TradeLineId,
            Amount = -alloc.Amount,
            ReversesAllocationId = alloc.Id,
        });
        audit.Add("payment.allocation_reverse", "payment_allocation", alloc.Id, new { alloc.Amount });
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// ADR-004 S10: hatali tahsilat ters tahsilat hareketiyle kapatilir. Kredi taksitine bagli odeme
    /// iptal edilince taksit yeniden "odenmemis" sayilir (LoanService). Tahsil edilmis bir ceke bagli
    /// tahsilat buradan iptal edilemez; cekin durumu ile tutarsizlik olur (Cek Portfoyu'nden yapilir).
    /// </summary>
    public Task<Payment> ReversePaymentAsync(Guid paymentId, string reason, CancellationToken ct) =>
        InTransactionAsync(() => ReversePaymentCoreAsync(paymentId, reason, allowLinkedCheck: false, ct), ct);

    /// <summary>Cek servisinin karsiliksiz akisi icin: cek durumu ayni transaction'da guncellenir.</summary>
    internal Task<Payment> ReverseCheckPaymentAsync(Guid paymentId, string reason, CancellationToken ct) =>
        InTransactionAsync(() => ReversePaymentCoreAsync(paymentId, reason, allowLinkedCheck: true, ct), ct);

    private async Task<Payment> ReversePaymentCoreAsync(Guid paymentId, string reason, bool allowLinkedCheck, CancellationToken ct)
    {
        var why = Clean(reason) ?? throw new DomainException("reason", "İptal gerekçesi zorunlu.");
        await LockAsync(LockTable.Payment, [paymentId], ct);
        var original = await db.Payments.SingleOrDefaultAsync(p => p.Id == paymentId, ct)
                       ?? throw new NotFoundException("Tahsilat/ödeme");
        if (original.ReversesPaymentId is not null)
            throw new DomainException("reverse_of_reverse", "Ters kaydın tersi alınamaz.");
        if (await db.Payments.AnyAsync(p => p.ReversesPaymentId == original.Id, ct))
            throw new DomainException("already_reversed", "Bu kayıt zaten iptal edilmiş.");
        if (!allowLinkedCheck && await db.Checks.AnyAsync(c => c.PaymentId == original.Id && c.Status == CheckStatus.Collected, ct))
            throw new DomainException("linked_check",
                "Bu kayıt tahsil edilmiş bir çeke bağlı. Geri almak için Çek Portföyü'ndeki çek işlemlerini kullanın.");

        // Acik dagitimlar once geri alinir.
        var allocations = await db.PaymentAllocations.Where(a => a.PaymentId == original.Id).ToListAsync(ct);
        var reversedIds = allocations.Where(a => a.ReversesAllocationId is not null).Select(a => a.ReversesAllocationId!.Value).ToHashSet();
        foreach (var a in allocations.Where(a => a.Amount > 0 && !reversedIds.Contains(a.Id)))
        {
            db.PaymentAllocations.Add(new PaymentAllocation
            {
                TenantId = a.TenantId, CreatedByUserId = user.UserId, PaymentId = a.PaymentId,
                TradeLineId = a.TradeLineId, Amount = -a.Amount, ReversesAllocationId = a.Id,
            });
        }

        var on = Today;
        var reversal = new Payment
        {
            TenantId = original.TenantId,
            CreatedByUserId = user.UserId,
            PartyId = original.PartyId,
            Direction = original.Direction,
            Method = original.Method,
            CashAccountId = original.CashAccountId,
            Amount = original.Amount,
            OccurredOn = on,
            Reference = original.Reference,
            Description = why,
            ReversesPaymentId = original.Id,
        };
        db.Payments.Add(reversal);

        var wasReceipt = original.Direction == PaymentDirection.Receipt;
        AddLedger(original.PartyId, on, LedgerSources.PaymentReversal, reversal.Id,
            debit: wasReceipt ? original.Amount : 0, credit: wasReceipt ? 0 : original.Amount,
            description: $"{(wasReceipt ? "Tahsilat" : "Ödeme")} iptali — {why}");

        audit.Add("payment.reverse", "payment", reversal.Id, new { originalPaymentId = original.Id, original.Amount, reason = why });
        await db.SaveChangesAsync(ct);
        return reversal;
    }

    // =====================================================================
    // Belge (elle fatura kaydi) — ADR-004, ADR-005
    // =====================================================================

    public Task<DocumentResult> EnterDocumentAsync(DocumentInput input, CancellationToken ct) =>
        InTransactionAsync(() => EnterDocumentCoreAsync(input, ct), ct);

    private async Task<DocumentResult> EnterDocumentCoreAsync(DocumentInput input, CancellationToken ct)
    {
        var tenantId = tenant.Require();
        var number = Clean(input.DocumentNo)?.ToUpperInvariant()
                     ?? throw new DomainException("document_no", "Fatura numarası zorunlu.");
        if (input.VatLines is not { Count: > 0 })
            throw new DomainException("vat_lines", "En az bir KDV satırı girilmeli (oran, matrah, KDV).");
        if (input.Links is not { Count: > 0 })
            throw new DomainException("links", "Faturanın hangi satışlara/alışlara ait olduğu seçilmeli.");
        if (input.Links.Select(l => l.TradeLineId).Distinct().Count() != input.Links.Count)
            throw new DomainException("links", "Aynı satır bir faturada iki kez seçilemez.");
        foreach (var v in input.VatLines)
        {
            if (v.VatRate is < 0 or > 100) throw new DomainException("vat_rate", "KDV oranı 0 ile 100 arasında olmalı.");
            if (!Money.HasAtMostTwoDecimals(v.NetAmount) || !Money.HasAtMostTwoDecimals(v.VatAmount))
                throw new DomainException("vat_amount", "Faturadaki tutarlar en fazla 2 ondalık basamaklı olmalı.");
        }
        if (input.GrossTotal < 0 || !Money.HasAtMostTwoDecimals(input.GrossTotal))
            throw new DomainException("gross", "Genel toplam geçersiz.");

        var party = await RequireActiveParty(input.PartyId, ct);
        var ids = input.Links.Select(l => l.TradeLineId).ToList();
        await LockAsync(LockTable.TradeLine, ids, ct);
        var lines = await db.TradeLines.Where(l => ids.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);

        var document = new Document
        {
            TenantId = tenantId,
            CreatedByUserId = user.UserId,
            PartyId = party.Id,
            Direction = input.Direction,
            Type = input.Type,
            DocumentNo = number,
            DocumentDate = input.DocumentDate,
            NetTotal = input.VatLines.Sum(v => v.NetAmount),
            VatTotal = input.VatLines.Sum(v => v.VatAmount),
            GrossTotal = input.GrossTotal,
            Note = Clean(input.Note),
        };

        decimal sysNet = 0, sysVat = 0, sysGross = 0;
        int? sign = null;
        foreach (var link in input.Links)
        {
            if (!lines.TryGetValue(link.TradeLineId, out var line)) throw new NotFoundException("Satır");
            if (line.PartyId != party.Id) throw new DomainException("links", "Fatura yalnız aynı carinin satırlarını kapsayabilir.");
            if (line.Direction != input.Direction) throw new DomainException("links", "Satış ve alış satırları aynı faturada olamaz.");
            var lineSign = Math.Sign(line.Quantity);
            sign ??= lineSign;
            if (lineSign != sign) throw new DomainException("links", "Satış ve iade satırları aynı faturada birleştirilemez; iade için ayrı iade faturası girilir.");

            var qty = Math.Abs(link.Quantity) * lineSign;
            var remaining = line.Quantity - line.DocumentedQuantity;
            if (qty == 0 || Math.Abs(qty) > Math.Abs(remaining))
                throw new DomainException("links", $"Seçilen miktar, satırın belgelenmemiş miktarını ({Math.Abs(remaining):0.####}) aşıyor.");

            line.DocumentedQuantity += qty;
            var ratio = qty / line.Quantity;
            sysNet += line.NetAmount * ratio;
            sysVat += line.VatAmount * ratio;
            sysGross += line.GrossAmount * ratio;

            document.Links.Add(new DocumentLink
            {
                TenantId = tenantId, CreatedByUserId = user.UserId, DocumentId = document.Id,
                TradeLineId = line.Id, Quantity = qty,
            });
        }
        foreach (var v in input.VatLines)
        {
            document.VatLines.Add(new DocumentVatLine
            {
                TenantId = tenantId, CreatedByUserId = user.UserId, DocumentId = document.Id,
                VatRate = v.VatRate, NetAmount = v.NetAmount, VatAmount = v.VatAmount,
            });
        }
        db.Documents.Add(document);

        var result = new DocumentResult(document.Id, input.GrossTotal,
            Money.Round(Math.Abs(sysNet)), Money.Round(Math.Abs(sysVat)), Money.Round(Math.Abs(sysGross)),
            input.GrossTotal - Money.Round(Math.Abs(sysGross)));

        audit.Add("document.enter", "document", document.Id, new
        {
            number, type = input.Type.ToString(), direction = input.Direction.ToString(), document.GrossTotal,
            systemGross = result.SystemGross, difference = result.Difference, links = input.Links.Count,
        });
        await db.SaveChangesAsync(ct);
        return result;
    }

    /// <summary>ADR-004 S5: iptal stok ve cariyi degistirmez; satirlar tekrar belgelenebilir hale gelir.</summary>
    public Task CancelDocumentAsync(Guid documentId, string reason, CancellationToken ct) =>
        InTransactionAsync(() => CancelDocumentCoreAsync(documentId, reason, ct), ct);

    private async Task CancelDocumentCoreAsync(Guid documentId, string reason, CancellationToken ct)
    {
        var why = Clean(reason) ?? throw new DomainException("reason", "İptal gerekçesi zorunlu.");
        await LockAsync(LockTable.Document, [documentId], ct);
        var document = await db.Documents.Include(d => d.Links).SingleOrDefaultAsync(d => d.Id == documentId, ct)
                       ?? throw new NotFoundException("Fatura");
        if (document.Status != DocumentStatus.Issued)
            throw new DomainException("already_cancelled", "Bu fatura zaten iptal edilmiş.");

        var lineIds = document.Links.Where(l => l.IsActive).Select(l => l.TradeLineId).ToList();
        await LockAsync(LockTable.TradeLine, lineIds, ct);
        var lines = await db.TradeLines.Where(l => lineIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, ct);
        foreach (var link in document.Links.Where(l => l.IsActive))
        {
            link.IsActive = false;
            lines[link.TradeLineId].DocumentedQuantity -= link.Quantity;
        }
        document.Status = DocumentStatus.Cancelled;
        document.CancelReason = why;
        document.CancelledAt = DateTimeOffset.UtcNow;
        document.CancelledByUserId = user.UserId;

        audit.Add("document.cancel", "document", document.Id, new { document.DocumentNo, reason = why });
        await db.SaveChangesAsync(ct);
    }

    // =====================================================================
    // Acilis bakiyesi
    // =====================================================================

    public async Task<OpeningBalance> CreateOpeningBalanceAsync(Guid partyId, decimal amount, string? note, CancellationToken ct)
    {
        if (amount == 0 || !Money.HasAtMostTwoDecimals(amount))
            throw new DomainException("amount", "Açılış bakiyesi sıfır olamaz ve en fazla 2 ondalık basamaklı olmalı.");
        var party = await RequireActiveParty(partyId, ct);
        var ob = new OpeningBalance
        {
            TenantId = tenant.Require(), CreatedByUserId = user.UserId, PartyId = party.Id,
            Amount = amount, Note = Clean(note),
        };
        db.OpeningBalances.Add(ob);
        audit.Add("opening_balance.create", "opening_balance", ob.Id, new { ob.PartyId, ob.Amount });
        await db.SaveChangesAsync(ct);
        return ob;
    }

    /// <summary>Acilis bakiyesi yalniz onayla cariye yazilir (ana hafiza 3.1).</summary>
    public Task DecideOpeningBalanceAsync(Guid id, bool approve, string? note, CancellationToken ct) =>
        InTransactionAsync(() => DecideOpeningBalanceCoreAsync(id, approve, note, ct), ct);

    private async Task DecideOpeningBalanceCoreAsync(Guid id, bool approve, string? note, CancellationToken ct)
    {
        await LockAsync(LockTable.OpeningBalance, [id], ct);
        var ob = await db.OpeningBalances.SingleOrDefaultAsync(o => o.Id == id, ct)
                 ?? throw new NotFoundException("Açılış bakiyesi");
        if (ob.Status != OpeningBalanceStatus.Pending)
            throw new DomainException("decided", "Bu açılış bakiyesi için daha önce karar verilmiş.");
        ob.Status = approve ? OpeningBalanceStatus.Approved : OpeningBalanceStatus.Rejected;
        ob.DecidedByUserId = user.UserId;
        ob.DecidedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(note)) ob.Note = Clean(note);
        if (approve)
        {
            AddLedger(ob.PartyId, Today, LedgerSources.OpeningBalance, ob.Id,
                debit: ob.Amount > 0 ? ob.Amount : 0, credit: ob.Amount < 0 ? -ob.Amount : 0,
                description: "Açılış bakiyesi");
        }
        audit.Add(approve ? "opening_balance.approve" : "opening_balance.reject", "opening_balance", ob.Id, new { ob.Amount });
        await db.SaveChangesAsync(ct);
    }

    // =====================================================================
    // Yardimcilar
    // =====================================================================

    public async Task<decimal> StockOnHandAsync(Guid itemId, CancellationToken ct) =>
        await db.StockMovements.Where(m => m.ItemId == itemId).SumAsync(m => (decimal?)m.Quantity, ct) ?? 0;

    private async Task AddStockMovement(Item item, decimal delta, Guid sourceId, DateOnly on, CancellationToken ct)
    {
        if (delta < 0)
        {
            // Ayni urunden es zamanli iki cikis stok sinirini birlikte asmasin diye urun satiri kilitlenir.
            await LockAsync(LockTable.Item, [item.Id], ct);
            var current = await StockOnHandAsync(item.Id, ct);
            if (current + delta < 0 && !user.IsInRole(Roles.Yonetici))
                throw new DomainException("negative_stock",
                    $"Stok yetersiz. Mevcut: {current:0.####} {item.Unit}. Eksi stoka düşürme yetkisi yalnız yöneticidedir.");
        }
        db.StockMovements.Add(new StockMovement
        {
            TenantId = item.TenantId, CreatedByUserId = user.UserId, ItemId = item.Id,
            Quantity = delta, SourceType = "trade_line", SourceId = sourceId, OccurredOn = on,
        });
    }

    private void AddLedger(Guid partyId, DateOnly on, string source, Guid sourceId, decimal debit, decimal credit, string description)
    {
        db.LedgerEntries.Add(new LedgerEntry
        {
            TenantId = tenant.Require(),
            CreatedByUserId = user.UserId,
            PartyId = partyId,
            OccurredOn = on,
            SourceType = source,
            SourceId = sourceId,
            Debit = debit,
            Credit = credit,
            Description = description.Length > 500 ? description[..500] : description,
        });
    }

    private async Task<Party> RequireActiveParty(Guid id, CancellationToken ct)
    {
        var party = await db.Parties.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException("Cari");
        if (!party.IsActive) throw new DomainException("party_inactive", "Cari kullanım dışı.");
        return party;
    }

    private static void EnsurePartyAllows(Party party, TradeDirection direction)
    {
        var ok = direction == TradeDirection.Sale
            ? party.Kind is PartyKind.Customer or PartyKind.Both
            : party.Kind is PartyKind.Supplier or PartyKind.Both;
        if (!ok)
            throw new DomainException("party_kind", direction == TradeDirection.Sale
                ? "Bu cari müşteri olarak tanımlı değil."
                : "Bu cari tedarikçi olarak tanımlı değil.");
    }

    /// <summary>
    /// Yon–arac–hesap matrisi (S-05). Musteri kart tahsilati yalniz POS'a; sirket kredi kartiyla yalniz odeme
    /// yapilir. Cek kurali ADR-007'ye kadar mevcut haliyle korunur.
    /// </summary>
    internal static void EnsureMethodMatchesAccount(PaymentDirection direction, PaymentMethod method, CashAccountKind kind)
    {
        var receipt = direction == PaymentDirection.Receipt;
        var ok = method switch
        {
            PaymentMethod.Cash => kind == CashAccountKind.Cash,
            PaymentMethod.Transfer => kind == CashAccountKind.Bank,
            PaymentMethod.Card => receipt ? kind == CashAccountKind.Pos : kind == CashAccountKind.CreditCard,
            PaymentMethod.Check => kind is CashAccountKind.Bank or CashAccountKind.Cash,
            _ => false,
        };
        if (!ok)
            throw new DomainException("account_kind", receipt
                ? "Tahsilat aracı ile hesap uyuşmuyor. Nakit → kasa, havale → banka, kart → POS, çek → banka veya kasa."
                : "Ödeme aracı ile hesap uyuşmuyor. Nakit → kasa, havale → banka, kart → şirket kredi kartı, çek → banka veya kasa.");
    }

    public static string MethodName(PaymentMethod m) => m switch
    {
        PaymentMethod.Cash => "Nakit",
        PaymentMethod.Transfer => "Havale/EFT",
        PaymentMethod.Card => "Kredi kartı/POS",
        PaymentMethod.Check => "Çek",
        _ => m.ToString(),
    };

    private static string? Clean(string? s)
    {
        var t = s?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
