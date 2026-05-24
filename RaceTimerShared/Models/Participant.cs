namespace RaceTimer.Shared.Models;

public class Participant
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTime? LastModifiedUtc { get; set; }
    public List<RaceParticipant> RaceParticipations { get; set; }
}
