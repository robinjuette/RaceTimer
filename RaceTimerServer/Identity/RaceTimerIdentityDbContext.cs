using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace RaceTimerServer.Identity;

public sealed class RaceTimerIdentityDbContext : IdentityDbContext<RaceTimerUser, IdentityRole<Guid>, Guid>
{
    public RaceTimerIdentityDbContext(DbContextOptions<RaceTimerIdentityDbContext> options) : base(options)
    {
    }

    public DbSet<RaceTimerUserParticipant> UserParticipants => Set<RaceTimerUserParticipant>();
    public DbSet<UserParticipantAudit> UserParticipantAudits => Set<UserParticipantAudit>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.UseOpenIddict();
        builder.Entity<RaceTimerUser>(entity => entity.Property(x => x.DisplayName).HasMaxLength(200));
        builder.Entity<RaceTimerUserParticipant>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.ParticipantId, x.CreatedAtUtc });
            entity.HasOne(x => x.User).WithMany(x => x.Participants).HasForeignKey(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.ParticipantId, x.RemovedAtUtc });
        });
        builder.Entity<UserParticipantAudit>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Action).HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.UserId, x.ParticipantId, x.OccurredAtUtc });
        });
    }
}
