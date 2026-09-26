using System.Runtime.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using UUSport.Api.Infrastructure;

namespace UUSport.Api.Data;

/// <summary>`dotnet ef` icin: migration rolu ile baglanir.</summary>
[SupportedOSPlatform("windows")]
public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(new DatabaseOptions().BuildConnectionString(asOwner: true))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(options);
    }
}
