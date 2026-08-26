namespace RaceTimerApp.Shared.Models;

public sealed class HostedServerOptions
{
    public string Mode { get; set; } = "";

    public string? ServerUrl { get; set; }

    public bool IsHosted => Mode.Equals("Hosted", StringComparison.OrdinalIgnoreCase);
}