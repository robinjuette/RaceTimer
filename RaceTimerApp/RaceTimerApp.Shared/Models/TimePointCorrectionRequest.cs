using System;

namespace RaceTimerApp.Shared.Models;

public class TimePointCorrectionRequest
{
    public Guid TimePointId { get; set; }
    public DateTime CurrentTimeUTC { get; set; }
    public DateTime? OriginalTimeUTC { get; set; }
    public DateTime CorrectedTimeUTC { get; set; }
    public string SelectedReason { get; set; } = string.Empty;
    public string? AdditionalNotes { get; set; }
    public bool IsCorrected { get; set; }
}

public class TimePointCorrectionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Guid TimePointId { get; set; }
}
