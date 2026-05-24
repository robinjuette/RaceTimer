using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RaceTimerServer.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<RaceTimerDbContext>
{
    public RaceTimerDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<RaceTimerDbContext>();
        // default to sqlite in project folder
        builder.UseSqlite("Data Source=Data/racetimer.db");
        return new RaceTimerDbContext(builder.Options);
    }
}
