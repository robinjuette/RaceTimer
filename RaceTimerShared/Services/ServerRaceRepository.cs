using RaceTimer.Shared.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace RaceTimer.Shared.Services
{
    public class ServerRaceRepository : IRaceRepository
    {
        private readonly RaceTimerApiClient raceTimerApiClient;

        public Task<Race?> AddRaceAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task<RaceParticipantTimePoint> AddUnassignedTimePointAsync(DateTime timePointUtc)
        {
            throw new NotImplementedException();
        }

        public Task<RaceParticipant> AssignParticipantToRaceAsync(Guid raceId, Guid participantId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AssignTimePointToRaceParticipantAsync(Guid timePointId, Guid raceId, Guid participantId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CopyRaceTimePointsAsync(Guid raceIdCopyFrom, Guid raceIdCopyTo)
        {
            throw new NotImplementedException();
        }

        public async Task<Participant?> TryFindParticipantAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task<Participant?> CreateParticipantAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task<RaceTimePoint?> CreateRaceTimePointAsync(Guid raceId, string? name)
        {
            throw new NotImplementedException();
        }

        public Task DeleteParticipantAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteRaceAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteRaceParticipantTimePointAsync(Guid timePointId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteRaceTimePointAsync(Guid timePointId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Participant>> GetAllParticipantsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Race>> GetAllRacesAsync()
        {
            throw new NotImplementedException();
        }

        public Task<object> GetChangesSinceAsync(Guid raceId, DateTime sinceUtc)
        {
            throw new NotImplementedException();
        }

        public Task<Participant?> GetParticipantAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<Race?> GetRaceAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<RaceParticipantTimePoint?> GetRaceParticipantTimePointAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RaceParticipantTimePoint>> GetRaceParticipantTimePointsForRaceAsync(Guid raceId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Race>> GetRacesByStatusAsync(RaceStatus status)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RaceParticipant>> GetRacesParticipantsAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RaceTimePoint>> GetRaceTimePointsAsync(Guid raceId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RaceParticipantTimePoint>?> GetUnassignedTimepointsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId)
        {
            throw new NotImplementedException();
        }

        public Task<bool> SetRaceParticipantTimePointPenaltyTime(Guid timePointId, TimeSpan penaltyTime)
        {
            throw new NotImplementedException();
        }

        public Task<bool> StartRaceAsync(Guid raceId, DateTime timePointUtc, params List<Guid> participantIds)
        {
            throw new NotImplementedException();
        }

        public Task UpdateParticipantAsync(Participant participant)
        {
            throw new NotImplementedException();
        }

        public Task UpdateRaceAsync(Race race)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateTimePointsAsync(Guid raceId, List<RaceTimePoint> timePoints)
        {
            throw new NotImplementedException();
        }

        public Task CheckForRaceCompletionAsync(Guid raceId)
        {
            throw new NotImplementedException();
        }
    }
}

