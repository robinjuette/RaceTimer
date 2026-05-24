using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using RaceTimer.Shared.Models;
using RaceTimerServer.Data;

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

    public async Task AssignParticipantToRaceAsync(Guid raceId, Guid participantId)
    {
        // determine smallest available participant number > 0 for this race
        var existingNumbers = await _db.RaceParticipants.Where(rp => rp.RaceID == raceId).Select(rp => rp.ParticipantNr).ToListAsync();
        int nr = 1;
        while (existingNumbers.Contains(nr)) nr++;
        var rp = new RaceParticipant { RaceID = raceId, ParticipantID = participantId, ParticipantNr = nr };
        _db.RaceParticipants.Add(rp);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("ParticipantAssigned", new object?[] { raceId, participantId });
    }

    public async Task RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId)
    {
        var rp = await _db.RaceParticipants.FindAsync(participantId, raceId);
        if (rp is null) return;
        _db.RaceParticipants.Remove(rp);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("ParticipantUnassigned", new object?[] { raceId, participantId });
    }

    public async Task AddTimePointForParticipantAsync(Guid raceId, Guid participantId, RaceParticipantTimePoint timePoint)
    {
        if (timePoint.Id == Guid.Empty) timePoint.Id = Guid.NewGuid();
        timePoint.RaceID = raceId;
        timePoint.ParticipantID = participantId;
        _db.RaceParticipantTimePoints.Add(timePoint);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("TimePointAdded", new object?[] { raceId, participantId, timePoint });
    }

    public async Task UpdateTimePointForParticipantAsync(RaceParticipantTimePoint timePoint)
    {
        var existing = await _db.RaceParticipantTimePoints.FindAsync(timePoint.Id);
        if (existing is null) return;
        _db.Entry(existing).CurrentValues.SetValues(timePoint);
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(timePoint.RaceID.ToString())).SendCoreAsync("TimePointUpdated", new object?[] { timePoint.RaceID, timePoint.ParticipantID, timePoint });
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
        var tp = await _db.RaceParticipantTimePoints.FindAsync(timePointId);
        if (tp is null) return;
        tp.ParticipantID = participantId;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName((tp.RaceID ?? Guid.Empty).ToString())).SendCoreAsync("TimePointAssigned", new object?[] { tp.RaceID, participantId, tp });
        // If the timepoint corresponds to the last RaceTimePoint for the race, set participant finish and possibly race finish
        if (tp.RaceID is not null && tp.RTPIndex is not null)
        {
            var raceId = tp.RaceID.Value;
            var lastRtp = await _db.RaceTimePoints.Where(r => r.RaceID == raceId).OrderByDescending(r => r.Index).FirstOrDefaultAsync();
            if (lastRtp is not null && tp.RTPIndex == lastRtp.Index)
            {
                // set participant finish
                var rp = await _db.RaceParticipants.FirstOrDefaultAsync(x => x.RaceID == raceId && x.ParticipantID == participantId);
                if (rp is not null)
                {
                    rp.FinishDateTimeUTC = tp.TimePointUTC;
                    await _db.SaveChangesAsync();
                    await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("ParticipantFinished", new object?[] { raceId, participantId, tp.TimePointUTC });
                }

                // if all participants have finishes, set race finish
                var allParticipants = await _db.RaceParticipants.Where(x => x.RaceID == raceId).ToListAsync();
                if (allParticipants.Count > 0 && allParticipants.All(x => x.FinishDateTimeUTC is not null))
                {
                    var race = await _db.Races.FindAsync(raceId);
                    if (race is not null && race.FinishDateTimeUTC is null)
                    {
                        race.FinishDateTimeUTC = tp.TimePointUTC;
                        await _db.SaveChangesAsync();
                        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("RaceFinished", new object?[] { raceId, tp.TimePointUTC });
                    }
                }
            }
        }
    }

    // Assign an existing (possibly unassigned) time point to a race
    public async Task AssignTimePointToRaceAsync(Guid timePointId, Guid raceId)
    {
        var tp = await _db.RaceParticipantTimePoints.FindAsync(timePointId);
        if (tp is null) return;
        tp.RaceID = raceId;
        await _db.SaveChangesAsync();
        await _hub.Clients.Group(RaceTimerServer.Hubs.RaceHub.GetGroupName(raceId.ToString())).SendCoreAsync("TimePointAssignedToRace", new object?[] { raceId, tp });
    }
}
