using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RaceTimer.Shared.Models;
using RaceTimerServer.Data;
using System.Diagnostics;

namespace RaceTimerServer.Services;

public class RaceRepository
{
    private readonly RaceTimerDbContext _db;
    private readonly Microsoft.AspNetCore.SignalR.IHubContext<RaceTimerServer.Hubs.RaceHub> _hub;

    public RaceRepository(RaceTimerDbContext db, Microsoft.AspNetCore.SignalR.IHubContext<RaceTimerServer.Hubs.RaceHub> hub)
    {
        _db = db;
        _hub = hub;
    }

    // Return changes since a given UTC timestamp for a specific race
    public async Task<object> GetChangesSinceAsync(Guid raceId, DateTime sinceUtc)
    {
        var races = await _db.Races.Where(r => r.Id == raceId && (r.LastModifiedUtc ?? DateTime.MinValue) > sinceUtc).ToListAsync();
        var participants = await _db.RaceParticipants.Where(rp => rp.RaceID == raceId && (rp.LastModifiedUtc ?? DateTime.MinValue) > sinceUtc).ToListAsync();
        var rtimepoints = await _db.RaceTimePoints.Where(rtp => rtp.RaceID == raceId && (rtp.LastModifiedUtc ?? DateTime.MinValue) > sinceUtc).ToListAsync();
        var rptps = await _db.RaceParticipantTimePoints.Where(rptp => (rptp.RaceID == raceId || rptp.RaceID == null) && (rptp.LastModifiedUtc ?? DateTime.MinValue) > sinceUtc).ToListAsync();

        return new
        {
            Races = races,
            RaceParticipants = participants,
            RaceTimePoints = rtimepoints,
            RaceParticipantTimePoints = rptps
        };
    }

    // Participants
    public async Task<IEnumerable<RaceTimer.Shared.Models.Participant>> GetParticipantsAsync()
    {
        return await _db.Participants.ToListAsync();
    }

    public async Task<RaceTimer.Shared.Models.Participant?> GetParticipantAsync(Guid id)
    {
        return await _db.Participants.FindAsync(id);
    }

    public async Task AddParticipantAsync(RaceTimer.Shared.Models.Participant participant)
    {
        if (participant.Id == Guid.Empty) participant.Id = Guid.NewGuid();
        _db.Participants.Add(participant);
        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendCoreAsync("ParticipantCreated", new object?[] { participant });
    }

    public async Task UpdateParticipantAsync(RaceTimer.Shared.Models.Participant participant)
    {
        var existing = await _db.Participants.FindAsync(participant.Id);
        if (existing is null) return;
        _db.Entry(existing).CurrentValues.SetValues(participant);
        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendCoreAsync("ParticipantUpdated", new object?[] { participant });
    }

    public async Task RemoveParticipantAsync(Guid id)
    {
        var existing = await _db.Participants.FindAsync(id);
        if (existing is null) return;
        _db.Participants.Remove(existing);
        await _db.SaveChangesAsync();
        await _hub.Clients.All.SendCoreAsync("ParticipantDeleted", new object?[] { id });
    }

    public async Task<IEnumerable<Race>> GetAllAsync()
    {
        return await _db.Races
            .Include(r => r.RaceParticipants)
            .Include(r => r.RaceTimePoints)
            .Include(r => r.RaceParticipantTimePoints)
            .ToListAsync();
    }

    public async Task<Race?> GetAsync(Guid id)
    {
        return await _db.Races
            .Include(r => r.RaceParticipants)
            .Include(r => r.RaceTimePoints)
            .Include(r => r.RaceParticipantTimePoints)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddAsync(Race race)
    {
        if (race.Id == Guid.Empty) race.Id = Guid.NewGuid();
        _db.Races.Add(race);
        await _db.SaveChangesAsync();
        // broadcast creation
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(race.Id.ToString())).SendCoreAsync("RaceCreated", new object?[] { race });
    }

    public async Task UpdateAsync(Race race)
    {
        var existing = await _db.Races.FindAsync(race.Id);
        if (existing is null) return;
        _db.Entry(existing).CurrentValues.SetValues(race);
        // for related collections you may need to handle updates explicitly
        try
        {
            await _db.SaveChangesAsync();
            await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(race.Id.ToString())).SendCoreAsync("RaceUpdated", new object?[] { race });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            // concurrency conflict - notify caller
            throw;
        }
    }

    public async Task RemoveAsync(Guid id)
    {
        var existing = await _db.Races.FindAsync(id);
        if (existing is null) return;
        _db.Races.Remove(existing);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(id.ToString())).SendCoreAsync("RaceDeleted", new object?[] { id });
    }

    public async Task<RaceParticipant> AssignParticipantToRaceAsync(Guid raceId, Guid participantId)
    {
        // determine smallest available participant number > 0 for this race
        var existingNumbers = await _db.RaceParticipants.Where(rp => rp.RaceID == raceId).Select(rp => rp.ParticipantNr).ToListAsync();
        int nr = 1;
        while (existingNumbers.Contains(nr)) nr++;
        var rp = new RaceParticipant { RaceID = raceId, ParticipantID = participantId, ParticipantNr = nr };
        _db.RaceParticipants.Add(rp);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("ParticipantAssigned", new object?[] { raceId, participantId });
        return rp;
    }

    public async Task RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId)
    {
        var rp = await _db.RaceParticipants.FindAsync(participantId, raceId);
        if (rp is null) return;
        _db.RaceParticipants.Remove(rp);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("ParticipantUnassigned", new object?[] { raceId, participantId });
    }

    public async Task<RaceParticipantTimePoint?> GetTimePointAsync(Guid id)
    {
        return await _db.RaceParticipantTimePoints.FindAsync(id);
    }

    public async Task<IEnumerable<Race>> GetRacesByStatusAsync(string status)
    {
        // status: prepared (StartTimeUTC == null && FinishDateTimeUTC == null)
        // running: StartTimeUTC != null && FinishDateTimeUTC == null
        // finished: FinishDateTimeUTC != null
        return status.ToLowerInvariant() switch
        {
            "prepared" => await _db.Races.Where(r => r.StartTimeUTC == null && r.FinishDateTimeUTC == null).ToListAsync(),
            "running" => await _db.Races.Where(r => r.StartTimeUTC != null && r.FinishDateTimeUTC == null).ToListAsync(),
            "finished" => await _db.Races.Where(r => r.FinishDateTimeUTC != null).ToListAsync(),
            _ => Enumerable.Empty<Race>()
        };
    }

    // Add an unassigned time point (only UTC timestamp) without race assignment
    public async Task<RaceParticipantTimePoint> AddUnassignedTimePointAsync(DateTime timePointUtc)
    {
        var tp = new RaceParticipantTimePoint
        {
            Id = Guid.NewGuid(),
            TimePointUTC = timePointUtc,
            RaceID = null,
            ParticipantID = null
        };
        _db.RaceParticipantTimePoints.Add(tp);
        await _db.SaveChangesAsync();
        // broadcast to all clients that an unassigned timepoint exists
        await _hub.Clients.All.SendCoreAsync("UnassignedTimePointAdded", new object?[] { tp });
        return tp;
    }

    // Assign an existing (possibly unassigned) time point to a participant
    public async Task AssignTimePointToParticipantAsync(Guid timePointId, Guid participantId)
    {
        RaceParticipantTimePoint? tp = await _db.RaceParticipantTimePoints.FindAsync(timePointId);
        if (tp is null) return;
        tp.ParticipantID = participantId;

        RaceParticipant? affectedRP = await _db.RaceParticipants
            .Where(rp => rp.Race.StartTimeUTC != null &&
                        rp.Race.FinishDateTimeUTC == null &&
                        rp.ParticipantID == participantId)
            .Include(rp => rp.RaceParticipantTimePoints)
            .Include(rp => rp.Race)
            .ThenInclude(r => r.RaceTimePoints)
            .SingleOrDefaultAsync();

        if(affectedRP is null) return;

        uint maxCurrentTPIndex = affectedRP.RaceParticipantTimePoints.Max(rptp => rptp.RTPIndex) ?? 0;

        uint nextIndex = affectedRP.Race.RaceTimePoints.Where(rtp => rtp.Index > maxCurrentTPIndex).Min(rtp => rtp.Index);
        uint maxIndex = affectedRP.Race.RaceTimePoints.Max(rtp => rtp.Index);

        tp.RaceID = affectedRP.RaceID;
        tp.RTPIndex = nextIndex;
        tp.ParticipantID = participantId;

        bool checkForCompletion = false;

        if(nextIndex == 1)
        {
            affectedRP.StartTime = tp.TimePointUTC;
        }
        if(nextIndex == maxIndex)
        {
            affectedRP.FinishDateTimeUTC = tp.TimePointUTC;
            checkForCompletion = true;
        }

        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName((tp.RaceID ?? Guid.Empty).ToString())).SendCoreAsync("TimePointAssigned", new object?[] { tp.RaceID, participantId, tp });

        if (checkForCompletion)
        {
            await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(tp.RaceID.ToString())).SendCoreAsync("ParticipantFinished", new object?[] { tp.RaceID, participantId, tp.TimePointUTC });
            await CheckForRaceCompletionAsync(affectedRP.RaceID);
        }
    }

    private async Task CheckForRaceCompletionAsync(Guid raceId)
    {
        Race? race = await _db.Races.FindAsync(raceId);

        if (race is null) return;

        if (!await _db.Races.Where(r => r.Id == raceId).AllAsync(r => r.RaceParticipants.All(rp => rp.FinishDateTimeUTC != null))) return;
        
        race.FinishDateTimeUTC = await _db.Races.Where(r => r.Id == raceId).Select(r => r.RaceParticipants.Max(rp => rp.FinishDateTimeUTC)).SingleAsync();

        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("RaceFinished", new object?[] { raceId, race.FinishDateTimeUTC });
    }

    internal async Task<IEnumerable<RaceParticipant>> GetRaceParticipantsAsync(Guid id)
    {
        return await _db.RaceParticipants.Where(rp => rp.RaceID == id).ToListAsync();
    }

    internal async Task<IEnumerable<RaceParticipantTimePoint>?> GetUnassignedTimepointsAsync()
    {
        return await _db.RaceParticipantTimePoints.Where(tp => tp.RaceID == null && tp.ParticipantID == null).ToListAsync();
    }

    internal async Task<IEnumerable<RaceParticipantTimePoint>> GetTimePointsForRaceAsync(Guid raceId)
    {
        return await _db.RaceParticipantTimePoints.Where(rp => rp.RaceID == raceId).ToListAsync();
    }

    internal async Task<bool> SetTimePointPenaltyTime(Guid timePointId, TimeSpan penaltyTime)
    {
        RaceParticipantTimePoint? timePoint = await _db.RaceParticipantTimePoints.FindAsync(timePointId);

        if (timePoint is null) return false;

        timePoint.PenaltyTime = penaltyTime;

        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(timePoint.RaceID.ToString())).SendCoreAsync("PenaltyTimeSet", new object?[] { timePointId, timePoint.PenaltyTime });

        return true;
    }

    internal async Task<RaceTimePoint?> AddTimePointAsync(Guid raceId, RaceTimePoint timePoint)
    {
        var race = await _db.Races.FindAsync(raceId);
        if (race is null) return null;

        // Determine next index
        var maxIndex = race.RaceTimePoints.Any() ? race.RaceTimePoints.Max(tp => tp.Index) : 0;
        timePoint.Index = maxIndex + 1;
        timePoint.Id = Guid.NewGuid();
        timePoint.RaceID = raceId;

        _db.RaceTimePoints.Add(timePoint);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("TimePointAdded", new object?[] { timePoint });
        return timePoint;
    }

    internal async Task RemoveTimePointAsync(Guid raceId, Guid timePointId)
    {
        var timePoint = await _db.RaceTimePoints.FindAsync(timePointId);
        if (timePoint is null) return;

        _db.RaceTimePoints.Remove(timePoint);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("TimePointRemoved", new object?[] { timePointId });
    }

    internal async Task<RaceTimePoint?> UpdateTimePointAsync(Guid raceId, RaceTimePoint timePoint)
    {
        var existing = await _db.RaceTimePoints.FindAsync(timePoint.Id);
        if (existing is null) return null;

        existing.DisplayName = timePoint.DisplayName;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("TimePointUpdated", new object?[] { existing });
        return existing;
    }

    internal void DeleteTimePointAsync(Guid timePointId)
    {
        var timePoint = _db.RaceParticipantTimePoints.Find(timePointId);
        if (timePoint is null) return;

        _db.RaceParticipantTimePoints.Remove(timePoint);
        _db.SaveChanges();
    }
}
