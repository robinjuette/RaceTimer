namespace RaceTimer.Shared.Models;

public class Race
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? StartTimeUTC { get; set; }
    public List<RaceParticipant> RaceParticipants { get; set; } = new();
    public List<RaceTimePoint> RaceTimePoints { get; set; } = new();
    public List<RaceParticipantTimePoint> RaceParticipantTimePoints { get; set; } = new();

    // Concurrency token for optimistic concurrency
    public byte[]? RowVersion { get; set; }
    public DateTime? FinishDateTimeUTC { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
}
