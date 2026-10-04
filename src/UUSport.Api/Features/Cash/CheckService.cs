using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Features.Audit;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Cash;

public sealed record CheckSeriesPreviewInput(string StartCheckNo, int Count, decimal Amount, DateOnly FirstDueDate, int DueDayInterval);

public sealed record CheckSeriesRow(string CheckNo, DateOnly DueDate, decimal Amount);

public sealed record CheckRowInput(string CheckNo, DateOnly DueDate, decimal Amount);

public sealed record CreateChecksInput(Guid PartyId, CheckDirection Direction, string BankName, List<CheckRowInput> Checks);

public sealed record CollectInput(Guid CashAccountId, DateOnly? OccurredOn);
public sealed record BounceInput(string Reason);
public sealed record EndorseInput(string? Note);

public sealed record CheckRow(
    Guid Id, Guid PartyId, string PartyName, string Direction, string CheckNo, string BankName, DateOnly DueDate,
    decimal Amount, string Status, Guid BatchId, Guid? PaymentId, string? StatusNote);

/// <summary>
/// Cek portfoyu (surum 2 hazirlik notu madde 3, spesifikasyon O37). Cek kaydi cariye
/// tutar yazmaz (ADR-004 ilkesi); tahsil edildiginde otomatik bir Payment uretilir.
/// </summary>
public sealed class CheckService(AppDbContext db, TenantContext tenant, ICurrentUser user, AuditWriter audit, AccountingService accounting)
{
    /// <summary>Seri cek onizlemesi: ardisik numaralarla N satir uretir; kaydetmez.</summary>
    public List<CheckSeriesRow> PreviewSeries(CheckSeriesPreviewInput input)
    {
        if (input.Count is <= 0 or > 500) throw new DomainException("count", "Çek adedi 1 ile 500 arasında olmalı.");
        var (prefix, digits, width) = SplitTrailingDigits(input.StartCheckNo);
        var rows = new List<CheckSeriesRow>(input.Count);
        for (var i = 0; i < input.Count; i++)
        {
            var no = prefix + (digits + i).ToString().PadLeft(width, '0');
            var due = input.DueDayInterval > 0 ? input.FirstDueDate.AddDays(input.DueDayInterval * i) : input.FirstDueDate;
            rows.Add(new CheckSeriesRow(no, due, input.Amount));
        }
        return rows;
    }

    private static (string Prefix, long Digits, int Width) SplitTrailingDigits(string checkNo)
    {
        var trimmed = (checkNo ?? string.Empty).Trim();
        var m = Regex.Match(trimmed, @"^(.*?)(\d+)$");
        if (!m.Success)
            throw new DomainException("check_no", "Seri çek için ilk çek numarası rakamla bitmeli (ör. AB000123).");
        var digitsText = m.Groups[2].Value;
        if (!long.TryParse(digitsText, out var digits))
            throw new DomainException("check_no", "Çek numarasındaki sayı bölümü çok uzun.");
        return (m.Groups[1].Value, digits, digitsText.Length);
    }

    /// <summary>Tekil veya seri (onizlemeden duzenlenmis) cek kaydi. Hepsi ayni BatchId'yi paylasir.</summary>
    public async Task<List<Check>> CreateAsync(CreateChecksInput input, CancellationToken ct)
    {
        var tenantId = tenant.Require();
        if (input.Checks is not { Count: > 0 }) throw new DomainException("checks", "En az bir çek girilmeli.");
        var bankName = input.BankName?.Trim();
        if (string.IsNullOrEmpty(bankName)) throw new DomainException("bank_name", "Banka adı zorunlu.");

        var party = await db.Parties.SingleOrDefaultAsync(p => p.Id == input.PartyId, ct) ?? throw new NotFoundException("Cari");
        if (!party.IsActive) throw new DomainException("party_inactive", "Cari kullanım dışı.");

        var seenNo = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in input.Checks)
        {
            if (string.IsNullOrWhiteSpace(c.CheckNo)) throw new DomainException("check_no", "Çek numarası zorunlu.");
            if (!seenNo.Add(c.CheckNo.Trim())) throw new DomainException("check_no", $"Çek numarası '{c.CheckNo}' bu istekte tekrar ediyor.");
            if (c.Amount <= 0) throw new DomainException("amount", "Çek tutarı sıfırdan büyük olmalı.");
        }

        var batchId = Guid.CreateVersion7();
        var checks = input.Checks.Select(c => new Check
        {
            TenantId = tenantId, CreatedByUserId = user.UserId, PartyId = party.Id, Direction = input.Direction,
            CheckNo = c.CheckNo.Trim(), BankName = bankName, DueDate = c.DueDate, Amount = c.Amount,
            Status = CheckStatus.InPortfolio, BatchId = batchId,
        }).ToList();
        db.Checks.AddRange(checks);

        audit.Add("check.create", "check_note", batchId, new
        {
            input.PartyId, direction = input.Direction.ToString(), bankName, count = checks.Count,
            total = checks.Sum(c => c.Amount), checkNos = checks.Select(c => c.CheckNo).ToList(),
        });
        await db.SaveChangesAsync(ct);
        return checks;
    }

    public Task SubmitForCollectionAsync(Guid id, CancellationToken ct) =>
        accounting.InTransactionAsync(async () =>
        {
            var check = await LockAndRequire(id, ct);
            if (check.Status != CheckStatus.InPortfolio)
                throw new DomainException("status", "Yalnız portföydeki çek tahsile verilebilir.");
            check.Status = CheckStatus.SubmittedForCollection;
            audit.Add("check.submit_for_collection", "check_note", check.Id);
            await db.SaveChangesAsync(ct);
        }, ct);

    /// <summary>
    /// Tahsil edildiginde otomatik Payment uretir; odeme ve cek durumu tek transaction'dadir (S-02).
    /// Gerceklesme tarihi verilmezse bugundur; vade tarihi nakit planinda kullanilir (O37, X:CSH-06).
    /// </summary>
    public Task CollectAsync(Guid id, CollectInput input, CancellationToken ct) =>
        accounting.InTransactionAsync(async () =>
        {
            var check = await LockAndRequire(id, ct);
            if (check.Status is not (CheckStatus.InPortfolio or CheckStatus.SubmittedForCollection))
                throw new DomainException("status", "Bu çek şu anki durumunda tahsil edilemez.");

            var direction = check.Direction == CheckDirection.Received ? PaymentDirection.Receipt : PaymentDirection.Disbursement;
            var method = check.Direction == CheckDirection.Received ? PaymentMethod.Check : PaymentMethod.Transfer;
            var payment = await accounting.CreatePaymentAsync(new PaymentInput(
                check.PartyId, direction, method, input.CashAccountId, check.Amount,
                input.OccurredOn ?? DateOnly.FromDateTime(DateTime.Now), check.CheckNo,
                $"Çek tahsilatı — {check.BankName} {check.CheckNo}", null), ct);

            check.Status = CheckStatus.Collected;
            check.PaymentId = payment.Id;
            audit.Add("check.collect", "check_note", check.Id, new { paymentId = payment.Id, check.Amount });
            await db.SaveChangesAsync(ct);
        }, ct);

    /// <summary>Karsiliksiz: daha once tahsil edilmisse (PaymentId dolu) o odeme ayni transaction'da ters kayitla geri alinir.</summary>
    public Task BounceAsync(Guid id, BounceInput input, CancellationToken ct) =>
        accounting.InTransactionAsync(async () =>
        {
            var check = await LockAndRequire(id, ct);
            if (check.Status == CheckStatus.Bounced) throw new DomainException("already_bounced", "Bu çek zaten karşılıksız olarak işaretli.");
            var reason = input.Reason?.Trim();
            if (string.IsNullOrEmpty(reason)) throw new DomainException("reason", "Karşılıksız gerekçesi zorunlu.");

            if (check.PaymentId is { } paymentId)
                await accounting.ReverseCheckPaymentAsync(paymentId, $"Çek karşılıksız çıktı: {reason}", ct);

            check.Status = CheckStatus.Bounced;
            check.StatusNote = reason;
            audit.Add("check.bounce", "check_note", check.Id, new { reason, hadPayment = check.PaymentId is not null });
            await db.SaveChangesAsync(ct);
        }, ct);

    public Task EndorseAsync(Guid id, EndorseInput input, CancellationToken ct) =>
        accounting.InTransactionAsync(async () =>
        {
            var check = await LockAndRequire(id, ct);
            if (check.Status is not (CheckStatus.InPortfolio or CheckStatus.SubmittedForCollection))
                throw new DomainException("status", "Yalnız portföydeki çek ciro edilebilir.");
            check.Status = CheckStatus.Endorsed;
            check.StatusNote = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
            audit.Add("check.endorse", "check_note", check.Id, new { note = check.StatusNote });
            await db.SaveChangesAsync(ct);
        }, ct);

    private async Task<Check> LockAndRequire(Guid id, CancellationToken ct)
    {
        await accounting.LockAsync(LockTable.CheckNote, [id], ct);
        return await Require(id, ct);
    }

    public async Task<List<CheckRow>> ListAsync(string? status, CancellationToken ct)
    {
        var q = db.Checks.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CheckStatus>(status, true, out var parsed))
            q = q.Where(c => c.Status == parsed);
        var list = await q.OrderBy(c => c.DueDate).ToListAsync(ct);
        if (list.Count == 0) return [];
        var partyIds = list.Select(c => c.PartyId).Distinct().ToList();
        var parties = await db.Parties.Where(p => partyIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        return list.Select(c => new CheckRow(c.Id, c.PartyId, parties.GetValueOrDefault(c.PartyId, ""),
            AppDbContext.ToSnake(c.Direction.ToString()), c.CheckNo, c.BankName, c.DueDate, c.Amount,
            AppDbContext.ToSnake(c.Status.ToString()), c.BatchId, c.PaymentId, c.StatusNote)).ToList();
    }

    private async Task<Check> Require(Guid id, CancellationToken ct) =>
        await db.Checks.SingleOrDefaultAsync(c => c.Id == id, ct) ?? throw new NotFoundException("Çek");
}
