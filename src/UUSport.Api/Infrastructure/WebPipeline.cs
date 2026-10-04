using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace UUSport.Api.Infrastructure;

/// <summary>
/// Hata eslemesi: kullaniciya kisa ve anlasilir mesaj, ayrinti loga (guvenlik standardi 7).
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (DomainException ex)
        {
            await Write(ctx, StatusCodes.Status400BadRequest, ex.Code, ex.Message);
        }
        catch (NotFoundException ex)
        {
            await Write(ctx, StatusCodes.Status404NotFound, "not_found", ex.Message);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            log.LogWarning("Benzersizlik ihlali: {Constraint}", pg.ConstraintName);
            await Write(ctx, StatusCodes.Status409Conflict, "duplicate", DuplicateMessage(pg.ConstraintName));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.CheckViolation } pg)
        {
            log.LogWarning("Kisit ihlali: {Constraint}", pg.ConstraintName);
            await Write(ctx, StatusCodes.Status400BadRequest, "rule_violation",
                "Kayıt bir iş kuralını ihlal ediyor ve kaydedilmedi.");
        }
        catch (BadHttpRequestException ex)
        {
            await Write(ctx, StatusCodes.Status400BadRequest, "bad_request", "İstek okunamadı: " + ex.Message);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Beklenmeyen hata");
            await Write(ctx, StatusCodes.Status500InternalServerError, "unexpected",
                "Beklenmeyen bir hata oluştu. Ayrıntı sistem kaydına yazıldı.");
        }
    }

    private static string DuplicateMessage(string? constraint) => constraint switch
    {
        "uq_document__tenant_sale_no" => "Bu fatura numarası zaten kayıtlı.",
        "uq_document__tenant_party_purchase_no" => "Bu tedarikçinin bu numaralı faturası zaten kayıtlı.",
        "uq_party__tenant_code" => "Bu cari kodu zaten kullanılıyor.",
        "uq_item__tenant_code" => "Bu ürün/hizmet kodu zaten kullanılıyor.",
        "uq_import_batch__tenant_kind_hash_applied" => "Bu dosya daha önce aktarıldı.",
        _ => "Aynı kayıt zaten var.",
    };

    private static async Task Write(HttpContext ctx, int status, string code, string message)
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new { code, message });
    }
}

/// <summary>
/// Guvenlik basliklari ve CSRF korumasi: API'ye yazan her istek ozel baslik tasimali
/// (SameSite=Strict cerezle birlikte).
/// </summary>
public sealed class SecurityMiddleware(RequestDelegate next)
{
    public const string RequestHeader = "X-UUSport-Request";

    public async Task InvokeAsync(HttpContext ctx, AppBuildInfo build)
    {
        var isApi = ctx.Request.Path.StartsWithSegments("/api");
        var buildId = isApi ? build.Current : null;
        // Basliklar yanit baslarken yazilir; hata yanitinda Response.Clear() onlari silemez.
        ctx.Response.OnStarting(() =>
        {
            var h = ctx.Response.Headers;
            h["X-Content-Type-Options"] = "nosniff";
            h["X-Frame-Options"] = "DENY";
            h["Referrer-Policy"] = "no-referrer";
            h["Content-Security-Policy"] =
                "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
            if (isApi) h["Cache-Control"] = "no-store";
            if (buildId is not null) h["X-App-Build"] = buildId;
            return Task.CompletedTask;
        });

        var isWrite = !HttpMethods.IsGet(ctx.Request.Method) && !HttpMethods.IsHead(ctx.Request.Method)
                      && !HttpMethods.IsOptions(ctx.Request.Method);
        if (isApi && isWrite && ctx.Request.Headers[RequestHeader] != "1")
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsJsonAsync(new { code = "csrf", message = "İstek reddedildi." });
            return;
        }
        await next(ctx);
    }
}
