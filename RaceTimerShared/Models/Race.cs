namespace RaceTimer.Shared.Models;

public class Race
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime? StartTimeUTC { get; set; }

    public List<RaceParticipant> RaceParticipants { get; set; }
    public List<RaceTimePoint> RaceTimePoints { get; set; }
    public List<RaceParticipantTimePoint> RaceParticipantTimePoints { get; set; }
}
