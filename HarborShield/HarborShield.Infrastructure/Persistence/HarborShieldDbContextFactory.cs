using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HarborShield.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations` run against this project directly (no Api startup wiring needed).
/// Uses the same local dev credentials as docker-compose.yml.
/// </summary>
public class HarborShieldDbContextFactory : IDesignTimeDbContextFactory<HarborShieldDbContext>
{
    public HarborShieldDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("HARBORSHIELD_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=harborshield;Username=harborshield;Password=harborshield";

        var optionsBuilder = new DbContextOptionsBuilder<HarborShieldDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.UseNetTopologySuite();
            npgsql.UseVector();
        });

        return new HarborShieldDbContext(optionsBuilder.Options);
    }
}
