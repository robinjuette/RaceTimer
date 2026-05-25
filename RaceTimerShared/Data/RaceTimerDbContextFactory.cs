using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RaceTimer.Shared.Data;

/// <summary>
/// Design-time factory für EF Core Migrations.
/// Wird von dotnet ef verwendet um Migrationen zu erstellen.
/// </summary>
public class RaceTimerDbContextFactory : IDesignTimeDbContextFactory<RaceTimerDbContext>
{
    public RaceTimerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RaceTimerDbContext>();
        var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RaceTimer");
        var dbPath = Path.Combine(basePath, "racetimer.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        return new RaceTimerDbContext(optionsBuilder.Options);
    }
}
