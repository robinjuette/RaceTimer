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
        var rp = new RaceParticipant { RaceID = raceId, ParticipantID = participantId };
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
}
