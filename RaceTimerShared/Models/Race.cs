namespace RaceTimer.Shared.Models;

public class Race
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public List<Participant> Participants { get; set; } = new();
}
