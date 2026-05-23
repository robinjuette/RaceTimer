namespace RaceTimer.Shared.Models;

public class Participant
{
    public Guid Id { get; set; }
    public string Bib { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
}
