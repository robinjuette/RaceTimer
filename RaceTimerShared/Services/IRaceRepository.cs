namespace RaceTimer.Shared.Services
{
    public interface IRaceRepository
    {
        Task<Race?> AddRaceAsync(string name);
        Task<RaceParticipantTimePoint> AddUnassignedTimePointAsync(DateTime timePointUtc);
        Task<RaceParticipant> AssignParticipantToRaceAsync(Guid raceId, Guid participantId);
        Task<bool> AssignTimePointToRaceParticipantAsync(Guid timePointId, Guid raceId, Guid participantId);
        Task<Participant?> CreateParticipantAsync(string name);
        Task DeleteParticipantAsync(Guid id);
        Task<bool> DeleteRaceAsync(Guid id);
        Task<bool> DeleteRaceParticipantTimePointAsync(Guid timePointId);
        Task<IEnumerable<Participant>> GetAllParticipantsAsync();
        Task<IEnumerable<Race>> GetAllRacesAsync();
        Task<object> GetChangesSinceAsync(Guid raceId, DateTime sinceUtc);
        Task<Participant?> TryFindParticipantAsync(string name);
        Task<Participant?> GetParticipantAsync(Guid id);
        Task<Race?> GetRaceAsync(Guid id);
        Task<IEnumerable<Race>> GetRacesByStatusAsync(RaceStatus status);
        Task<IEnumerable<RaceParticipant>> GetRacesParticipantsAsync(Guid id);
        Task<RaceParticipantTimePoint?> GetRaceParticipantTimePointAsync(Guid id);
        Task<IEnumerable<RaceParticipantTimePoint>> GetRaceParticipantTimePointsForRaceAsync(Guid raceId);
        Task<IEnumerable<RaceParticipantTimePoint>?> GetUnassignedTimepointsAsync();
        Task<bool> RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId);
        Task<bool> SetRaceParticipantTimePointPenaltyTime(Guid timePointId, TimeSpan penaltyTime);
        Task<bool> StartRaceAsync(Guid raceId, DateTime timePointUtc, params List<Guid> participantIds);
        Task UpdateParticipantAsync(Participant participant);
        Task UpdateRaceAsync(Race race);
        Task<bool> UpdateTimePointsAsync(Guid raceId, List<RaceTimePoint> timePoints);
        Task<bool> CopyRaceTimePointsAsync(Guid raceIdCopyFrom, Guid raceIdCopyTo);
        Task<IEnumerable<RaceTimePoint>> GetRaceTimePointsAsync(Guid raceId);
        Task<RaceTimePoint?> CreateRaceTimePointAsync(Guid raceId, string? name);
        Task<bool> DeleteRaceTimePointAsync(Guid timePointId);
        Task CheckForRaceCompletionAsync(Guid raceId);
    }
}