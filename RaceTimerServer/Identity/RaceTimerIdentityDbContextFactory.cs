using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace RaceTimerServer.Identity;

public sealed class RaceTimerIdentityDbContextFactory : IDesignTimeDbContextFactory<RaceTimerIdentityDbContext>
{
    public RaceTimerIdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<RaceTimerIdentityDbContext>()
            .UseNpgsql("Host=localhost;Database=racetimer;Username=racetimer;Password=design-time")
            .Options;
        return new RaceTimerIdentityDbContext(options);
    }
}
