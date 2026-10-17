using UUSport.Api.Features.Auth;

namespace UUSport.Api.Features.Cash;

public static class LoanEndpoints
{
    public static void MapLoans(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/loans").RequireAuthorization(Policies.Staff).RequirePermission(Permissions.CashRead);

        g.MapGet("", async (LoanService s, CancellationToken ct) => Results.Ok(await s.ListAsync(ct)));

        g.MapGet("/{id:guid}/installments", async (Guid id, LoanService s, CancellationToken ct) =>
            Results.Ok(await s.InstallmentsAsync(id, ct)));

        g.MapPost("", async (LoanInput input, LoanService s, CancellationToken ct) =>
        {
            var loan = await s.CreateLoanAsync(input, ct);
            return Results.Ok(new { loan.Id });
        }).RequireAuthorization(Policies.Admin);

        g.MapPost("/{id:guid}/installments/{no:int}/pay", async (Guid id, int no, PayInstallmentInput input, LoanService s, CancellationToken ct) =>
        {
            await s.PayInstallmentAsync(id, no, input, ct);
            return Results.Ok(new { ok = true });
        });
    }
}
