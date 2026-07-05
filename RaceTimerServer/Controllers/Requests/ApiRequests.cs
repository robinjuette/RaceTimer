namespace RaceTimerServer.Controllers.Requests;

public class CreateRaceRequest
{
    public string Name { get; set; } = string.Empty;
}

public class CreateParticipantRequest
{
    public string Name { get; set; } = string.Empty;
}

public class CreateTimePointRequest
{
    public string? Name { get; set; }
}

public class CreateUnassignedTimePointRequest
{
    public DateTime TimePointUtc { get; set; }
}

public class StartRaceRequest
{
    public IEnumerable<Guid> ParticipantIds { get; set; } = [];
    public DateTime? TimePointUtc { get; set; }
}

public class CorrectTimePointRequest
{
    public DateTime CorrectedTimeUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? CorrectedByUser { get; set; }
}
