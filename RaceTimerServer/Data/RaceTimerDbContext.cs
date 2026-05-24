using Microsoft.EntityFrameworkCore;
using RaceTimer.Shared.Models;

namespace RaceTimerServer.Data;

public class RaceTimerDbContext : DbContext
{
    public RaceTimerDbContext(DbContextOptions<RaceTimerDbContext> options) : base(options)
    {
    }

    public DbSet<Race> Races { get; set; }
    public DbSet<Participant> Participants { get; set; }
    public DbSet<RaceParticipant> RaceParticipants { get; set; }
    public DbSet<RaceTimePoint> RaceTimePoints { get; set; }
    public DbSet<RaceParticipantTimePoint> RaceParticipantTimePoints { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Race>(b =>
        {
            b.HasKey(r => r.Id);
            b.HasMany(r => r.RaceParticipants).WithOne(rp => rp.Race).HasForeignKey(rp => rp.RaceID);
            b.HasMany(r => r.RaceTimePoints).WithOne(rtp => rtp.Race).HasForeignKey(rtp => rtp.RaceID);
            b.HasMany(r => r.RaceParticipantTimePoints).WithOne(rptp => rptp.Race).HasForeignKey(rptp => rptp.RaceID);
            b.Property(r => r.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<Participant>(b =>
        {
            b.HasKey(p => p.Id);
        });

        modelBuilder.Entity<RaceParticipant>(b =>
        {
            b.HasKey(rp => new { rp.ParticipantID, rp.RaceID });
            b.HasOne(rp => rp.Participant).WithMany();
        });

        modelBuilder.Entity<RaceTimePoint>(b =>
        {
            b.HasKey(rtp => rtp.Id);
            b.HasOne(rtp => rtp.Race).WithMany(r => r.RaceTimePoints).HasForeignKey(rtp => rtp.RaceID);
        });

        modelBuilder.Entity<RaceParticipantTimePoint>(b =>
        {
            b.HasKey(rptp => rptp.Id);
            b.HasOne(rptp => rptp.Participant).WithMany();
            b.HasOne(rptp => rptp.Race).WithMany(r => r.RaceParticipantTimePoints).HasForeignKey(rptp => rptp.RaceID);
        });
    }
}
