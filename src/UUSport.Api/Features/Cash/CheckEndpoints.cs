using UUSport.Api.Features.Auth;

namespace UUSport.Api.Features.Cash;

public static class CheckEndpoints
{
    public static void MapChecks(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/checks").RequireAuthorization(Policies.Staff).RequirePermission(Permissions.CashRead);

        g.MapGet("", async (string? status, CheckService s, CancellationToken ct) => Results.Ok(await s.ListAsync(status, ct)));

        // Seri cek onizlemesi: kaydetmez, yalniz ardisik numarali satirlari onerir (madde 3).
        g.MapPost("/preview-series", (CheckSeriesPreviewInput input, CheckService s) => Results.Ok(s.PreviewSeries(input)));

        g.MapPost("", async (CreateChecksInput input, CheckService s, CancellationToken ct) =>
        {
            var checks = await s.CreateAsync(input, ct);
            return Results.Ok(new { batchId = checks[0].BatchId, count = checks.Count });
        });

        g.MapPost("/{id:guid}/submit-for-collection", async (Guid id, CheckService s, CancellationToken ct) =>
        {
            await s.SubmitForCollectionAsync(id, ct);
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/{id:guid}/collect", async (Guid id, CollectInput input, CheckService s, CancellationToken ct) =>
        {
            await s.CollectAsync(id, input, ct);
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/{id:guid}/bounce", async (Guid id, BounceInput input, CheckService s, CancellationToken ct) =>
        {
            await s.BounceAsync(id, input, ct);
            return Results.Ok(new { ok = true });
        });

        g.MapPost("/{id:guid}/endorse", async (Guid id, EndorseInput input, CheckService s, CancellationToken ct) =>
        {
            await s.EndorseAsync(id, input, ct);
            return Results.Ok(new { ok = true });
        });
    }
}
