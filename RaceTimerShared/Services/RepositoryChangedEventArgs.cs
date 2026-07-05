using System;

namespace RaceTimerShared.Services
{
    public enum RepositoryChangeType
    {
        Created,
        Updated,
        Deleted,
        Assigned,
        Unassigned,
        Started,
        Finished,
        Corrected,
        Undone,
        Custom
    }

    public class RepositoryChangedEventArgs : EventArgs
    {
        public RepositoryChangeType ChangeType { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public object? Payload { get; set; }
    }
}
