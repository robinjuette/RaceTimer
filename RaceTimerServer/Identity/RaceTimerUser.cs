using Microsoft.AspNetCore.Identity;

namespace RaceTimerServer.Identity;

public sealed class RaceTimerUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginUtc { get; set; }
    public ICollection<RaceTimerUserParticipant> Participants { get; set; } = [];
}

public sealed class RaceTimerUserParticipant
{
    public Guid UserId { get; set; }
    public RaceTimerUser User { get; set; } = null!;
    public Guid ParticipantId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? RemovedAtUtc { get; set; }
    public Guid? RemovedByUserId { get; set; }
}
