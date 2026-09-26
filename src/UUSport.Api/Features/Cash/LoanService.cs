using Microsoft.EntityFrameworkCore;
using UUSport.Api.Data;
using UUSport.Api.Domain;
using UUSport.Api.Features.Accounting;
using UUSport.Api.Features.Audit;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Features.Cash;

public sealed record LoanInstallmentInput(int InstallmentNo, DateOnly DueDate, decimal PrincipalAmount, decimal InterestAmount, decimal FeeAmount);

public sealed record LoanInput(
    Guid PartyId, Guid CashAccountId, string Name, decimal PrincipalAmount, DateOnly StartDate,
    string? Notes, List<LoanInstallmentInput> Installments);

public sealed record PayInstallmentInput(Guid CashAccountId, PaymentMethod Method, DateOnly? OccurredOn, string? Reference);

public sealed record LoanRow(
    Guid Id, Guid PartyId, string PartyName, Guid CashAccountId, string CashAccountName, string Name,
    decimal PrincipalAmount, DateOnly StartDate, bool IsActive, int InstallmentCount, int PaidCount,
    decimal RemainingPrincipal, DateOnly? NextDueDate);

public sealed record LoanInstallmentRow(
    Guid Id, int InstallmentNo, DateOnly DueDate, decimal PrincipalAmount, decimal InterestAmount,
    decimal FeeAmount, decimal TotalAmount, bool IsPaid, Guid? PaymentId);

/// <summary>
/// Kredi ve taksit tablosu (surum 2 hazirlik notu madde 3). Kredi veren banka/kurum
/// bir Party'dir; taksit odemesi ADR-004'un cari hareket modelini degistirmeden
/// AccountingService.CreatePaymentAsync uzerinden normal bir Payment(Disbursement)
/// olarak islenir. CashAccountId (Kind=Loan) yalniz gruplama/rapor icindir.
/// </summary>
public sealed class LoanService(AppDbContext db, TenantContext tenant, ICurrentUser user, AuditWriter audit, AccountingService accounting)
{
    public async Task<Loan> CreateLoanAsync(LoanInput input, CancellationToken ct)
    {
        var tenantId = tenant.Require();
        if (input.PrincipalAmount <= 0) throw new DomainException("principal", "Kredi ana parası sıfırdan büyük olmalı.");
        if (input.Installments is not { Count: > 0 }) throw new DomainException("installments", "En az bir taksit girilmeli.");

        var party = await db.Parties.SingleOrDefaultAsync(p => p.Id == input.PartyId, ct) ?? throw new NotFoundException("Cari");
        if (!party.IsActive) throw new DomainException("party_inactive", "Cari kullanım dışı.");
        if (party.Kind is not (PartyKind.Supplier or PartyKind.Both))
            throw new DomainException("party_kind", "Krediyi veren kurum tedarikçi (veya her ikisi) türünde bir cari olmalı.");

        var account = await db.CashAccounts.SingleOrDefaultAsync(a => a.Id == input.CashAccountId, ct)
                      ?? throw new NotFoundException("Kredi hesabı");
        if (account.Kind != CashAccountKind.Loan)
            throw new DomainException("account_kind", "Seçilen hesap Kredi türünde olmalı.");
        if (account.ParentAccountId is null)
            throw new DomainException("account_parent", "Kredi hesabı bir banka hesabının altında tanımlanmalı.");

        var seen = new HashSet<int>();
        decimal principalSum = 0, interestSum = 0, feeSum = 0;
        foreach (var i in input.Installments)
        {
            if (i.InstallmentNo <= 0) throw new DomainException("installment_no", "Taksit numarası sıfırdan büyük olmalı.");
            if (!seen.Add(i.InstallmentNo)) throw new DomainException("installment_no", $"Taksit numarası {i.InstallmentNo} tekrar ediyor.");
            if (i.PrincipalAmount < 0 || i.InterestAmount < 0 || i.FeeAmount < 0)
                throw new DomainException("installment_amount", "Taksit tutarları eksi olamaz.");
            principalSum += i.PrincipalAmount;
            interestSum += i.InterestAmount;
            feeSum += i.FeeAmount;
        }
        if (Money.Round(principalSum) != Money.Round(input.PrincipalAmount))
            throw new DomainException("principal_mismatch",
                $"Taksitlerin ana para toplamı ({Money.Round(principalSum):N2}) kredi ana parasına ({input.PrincipalAmount:N2}) eşit değil.");

        var loan = new Loan
        {
            TenantId = tenantId, CreatedByUserId = user.UserId, PartyId = party.Id, CashAccountId = account.Id,
            Name = input.Name.Trim(), PrincipalAmount = input.PrincipalAmount, StartDate = input.StartDate,
            Notes = string.IsNullOrWhiteSpace(input.Notes) ? null : input.Notes.Trim(),
        };
        if (loan.Name.Length == 0) throw new DomainException("name", "Kredi adı zorunlu.");
        db.Loans.Add(loan);

        foreach (var i in input.Installments)
        {
            db.LoanInstallments.Add(new LoanInstallment
            {
                TenantId = tenantId, CreatedByUserId = user.UserId, LoanId = loan.Id, InstallmentNo = i.InstallmentNo,
                DueDate = i.DueDate, PrincipalAmount = i.PrincipalAmount, InterestAmount = i.InterestAmount, FeeAmount = i.FeeAmount,
            });
        }

        audit.Add("loan.create", "loan", loan.Id, new
        {
            loan.PartyId, loan.CashAccountId, loan.PrincipalAmount, installmentCount = input.Installments.Count,
            interestTotal = interestSum, feeTotal = feeSum,
        });
        await db.SaveChangesAsync(ct);
        return loan;
    }

    /// <summary>Taksit odemesi ADR-004'u degistirmez: normal bir Payment(Disbursement) uretir.</summary>
    public async Task PayInstallmentAsync(Guid loanId, int installmentNo, PayInstallmentInput input, CancellationToken ct)
    {
        var loan = await db.Loans.SingleOrDefaultAsync(l => l.Id == loanId, ct) ?? throw new NotFoundException("Kredi");
        var installment = await db.LoanInstallments.SingleOrDefaultAsync(i => i.LoanId == loanId && i.InstallmentNo == installmentNo, ct)
                          ?? throw new NotFoundException("Taksit");
        if (installment.PaymentId is not null) throw new DomainException("already_paid", "Bu taksit zaten ödenmiş.");

        var total = Money.Round(installment.PrincipalAmount + installment.InterestAmount + installment.FeeAmount);
        var payment = await accounting.CreatePaymentAsync(new PaymentInput(
            loan.PartyId, PaymentDirection.Disbursement, input.Method, input.CashAccountId, total,
            input.OccurredOn ?? installment.DueDate, input.Reference,
            $"Kredi taksiti {installmentNo} — {loan.Name}", null), ct);

        installment.PaymentId = payment.Id;
        audit.Add("loan.installment_pay", "loan_installment", installment.Id, new { loanId, installmentNo, total, paymentId = payment.Id });
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<LoanRow>> ListAsync(CancellationToken ct)
    {
        var loans = await db.Loans.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
        if (loans.Count == 0) return [];
        var ids = loans.Select(l => l.Id).ToList();
        var partyIds = loans.Select(l => l.PartyId).Distinct().ToList();
        var accountIds = loans.Select(l => l.CashAccountId).Distinct().ToList();
        var parties = await db.Parties.Where(p => partyIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        var accounts = await db.CashAccounts.Where(a => accountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Name, ct);
        var installments = await db.LoanInstallments.Where(i => ids.Contains(i.LoanId)).ToListAsync(ct);
        var byLoan = installments.GroupBy(i => i.LoanId).ToDictionary(g => g.Key, g => g.ToList());

        return loans.Select(l =>
        {
            var list = byLoan.GetValueOrDefault(l.Id, []);
            var unpaid = list.Where(i => i.PaymentId is null).OrderBy(i => i.DueDate).ToList();
            return new LoanRow(l.Id, l.PartyId, parties.GetValueOrDefault(l.PartyId, ""), l.CashAccountId,
                accounts.GetValueOrDefault(l.CashAccountId, ""), l.Name, l.PrincipalAmount, l.StartDate, l.IsActive,
                list.Count, list.Count - unpaid.Count, unpaid.Sum(i => i.PrincipalAmount), unpaid.FirstOrDefault()?.DueDate);
        }).ToList();
    }

    public async Task<List<LoanInstallmentRow>> InstallmentsAsync(Guid loanId, CancellationToken ct) =>
        await db.LoanInstallments.Where(i => i.LoanId == loanId).OrderBy(i => i.InstallmentNo)
            .Select(i => new LoanInstallmentRow(i.Id, i.InstallmentNo, i.DueDate, i.PrincipalAmount, i.InterestAmount,
                i.FeeAmount, i.PrincipalAmount + i.InterestAmount + i.FeeAmount, i.PaymentId != null, i.PaymentId))
            .ToListAsync(ct);
}
