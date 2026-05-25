using Microsoft.EntityFrameworkCore;
using RaceTimer.Shared.Data;
using RaceTimer.Shared.Models;
using System.Diagnostics;

namespace RaceTimer.Shared.Services;

public class CoreRaceRepository : IRaceRepository
{
    private readonly IDbContextFactory<RaceTimerDbContext> dbContextFactory;

    private TaskCompletionSource? migrationCheckTCS;

    public CoreRaceRepository(IDbContextFactory<RaceTimerDbContext> dbContextFactory)
    {
        this.dbContextFactory = dbContextFactory;

        _ = CheckAndApplyMigrationsAsync();
    }

    private async Task CheckAndApplyMigrationsAsync()
    {
        if (migrationCheckTCS?.Task.IsCompleted == true)
        {
            return;
        }
        if(migrationCheckTCS != null)
        {
            await migrationCheckTCS.Task;
        }

        migrationCheckTCS = new();

        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        if((await _db.Database.GetPendingMigrationsAsync()).Any())
        {
            await _db.Database.MigrateAsync();
        }

        migrationCheckTCS.SetResult();
    }

    // Return changes since a given UTC timestamp for a specific race
    public async Task<object> GetChangesSinceAsync(Guid raceId, DateTime sinceUtc)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

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
    public async Task<IEnumerable<Participant>> GetAllParticipantsAsync()
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        return await _db.Participants.ToListAsync();
    }

    public async Task<Participant?> GetParticipantAsync(Guid id)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        return await _db.Participants.FindAsync(id);
    }

    public async Task<Participant?> CreateParticipantAsync(string name)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        if (await _db.Participants.AnyAsync(p => p.DisplayName.Equals(name))) return null;

        Participant participant = new()
        {
            DisplayName = name,
            Id = Guid.NewGuid(),
        };
        _db.Participants.Add(participant);
        await _db.SaveChangesAsync();

        return participant;
    }

    public async Task UpdateParticipantAsync(Participant participant)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        var existing = await _db.Participants.FindAsync(participant.Id);
        if (existing is null) return;
        _db.Entry(existing).CurrentValues.SetValues(participant);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteParticipantAsync(Guid id)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        var existing = await _db.Participants.FindAsync(id);
        if (existing is null) return;
        _db.Participants.Remove(existing);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<Race>> GetAllRacesAsync()
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        return await _db.Races
            .Include(r => r.RaceParticipants)
            .Include(r => r.RaceTimePoints)
            .Include(r => r.RaceParticipantTimePoints)
            .ToListAsync();
    }

    public async Task<Race?> GetRaceAsync(Guid id)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        return await _db.Races
            .Include(r => r.RaceParticipants)
            .Include(r => r.RaceTimePoints)
            .Include(r => r.RaceParticipantTimePoints)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Race?> AddRaceAsync(string name)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        if (await _db.Races.AnyAsync(r => r.Name.Equals(name)))
        {
            return null;
        }

        Race newRace = new()
        {
            Id = Guid.NewGuid(),
            Name = name,
        };

        newRace.RaceTimePoints.Add(new() { DisplayName = "Start", Index = 1 });

        _db.Races.Add(newRace);
        await _db.SaveChangesAsync();

        return newRace;
    }

    public async Task UpdateRaceAsync(Race race)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        // TODO: Ensure RaceTimePoints Updated

        var existing = await _db.Races.FindAsync(race.Id);
        if (existing is null) return;
        _db.Entry(existing).CurrentValues.SetValues(race);
        // for related collections you may need to handle updates explicitly
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            // concurrency conflict - notify caller
            throw;
        }
    }

    public async Task<bool> DeleteRaceAsync(Guid id)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        var existing = await _db.Races.FindAsync(id);
        if (existing is null) return false;
        var rptps = await _db.RaceParticipantTimePoints.Where(rtp => rtp.RaceID == id).ToListAsync();
        var rtps = await _db.RaceTimePoints.Where(rtp => rtp.RaceID == id).ToListAsync();
        var rps = await _db.RaceParticipants.Where(rtp => rtp.RaceID == id).ToListAsync();

        if (rptps.Count != 0)
        {
            _db.RemoveRange(rptps);
        }
        if (rps.Count != 0)
        {
            _db.RemoveRange(rps);
        }
        if (rtps.Count != 0)
        {
            _db.RemoveRange(rtps);
        }
        _db.Races.Remove(existing);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<RaceParticipant> AssignParticipantToRaceAsync(Guid raceId, Guid participantId)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        // determine smallest available participant number > 0 for this race
        var existingNumbers = await _db.RaceParticipants.Where(rp => rp.RaceID == raceId).Select(rp => rp.ParticipantNr).ToListAsync();
        int nr = 1;
        while (existingNumbers.Contains(nr)) nr++;
        var rp = new RaceParticipant { RaceID = raceId, ParticipantID = participantId, ParticipantNr = nr };
        _db.RaceParticipants.Add(rp);
        await _db.SaveChangesAsync();
        return rp;
    }

    public async Task<bool> RemoveParticipantFromRaceAsync(Guid raceId, Guid participantId)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        //TODO: Delete existing RaceParticipantTimePoints for RaceParticipant

        var rp = await _db.RaceParticipants.FindAsync(participantId, raceId);
        if (rp is null) return false;
        _db.RaceParticipants.Remove(rp);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<RaceParticipantTimePoint?> GetRaceParticipantTimePointAsync(Guid id)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        return await _db.RaceParticipantTimePoints.FindAsync(id);
    }

    public async Task<IEnumerable<Race>> GetRacesByStatusAsync(RaceStatus status)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        // status: prepared (StartTimeUTC == null && FinishDateTimeUTC == null)
        // running: StartTimeUTC != null && FinishDateTimeUTC == null
        // finished: FinishDateTimeUTC != null
        return status switch
        {
            RaceStatus.Planned => await _db.Races.Where(r => r.StartTimeUTC == null && r.FinishDateTimeUTC == null).ToListAsync(),
            RaceStatus.Running => await _db.Races.Where(r => r.StartTimeUTC != null && r.FinishDateTimeUTC == null).ToListAsync(),
            RaceStatus.Finished => await _db.Races.Where(r => r.FinishDateTimeUTC != null).ToListAsync(),
            _ => throw new ArgumentException()
        };
    }

    // Add an unassigned time point (only UTC timestamp) without race assignment
    public async Task<RaceParticipantTimePoint> AddUnassignedTimePointAsync(DateTime timePointUtc)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

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
        return tp;
    }

    public async Task<bool> StartRaceAsync(Guid raceId, DateTime timePointUtc, params List<Guid> participantIds)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        Dictionary<Guid, RaceParticipant> foundRPS = await _db.RaceParticipants
            .Where(rp => rp.RaceID == raceId && participantIds.Contains(rp.ParticipantID))
            .Include(rp => rp.RaceParticipantTimePoints)
            .Include(rp => rp.Race)
            .ThenInclude(r => r.RaceTimePoints)
            .ToDictionaryAsync(rp => rp.ParticipantID, rp => rp);

        if (foundRPS.Count == 0 ||
            !participantIds.All(foundRPS.ContainsKey) ||
            foundRPS.Any(kvp => kvp.Value.StartTime != null || kvp.Value.RaceParticipantTimePoints.Count != 0))
        {
            return false;
        }

        Race race = foundRPS.First().Value.Race;

        if (race.StartTimeUTC == null)
        {
            race.StartTimeUTC = timePointUtc;

            await _db.SaveChangesAsync();
        }

        foreach (var kvp in foundRPS)
        {
            kvp.Value.StartTime = timePointUtc;
            RaceParticipantTimePoint raceParticipantTimePoint = new()
            {
                LastModifiedUtc = DateTime.UtcNow,
                TimePointUTC = timePointUtc,
                RaceID = raceId,
                ParticipantID = kvp.Key,
                RTPIndex = 1
            };

            _db.RaceParticipantTimePoints.Add(raceParticipantTimePoint);
            await _db.SaveChangesAsync();
        }

        return true;
    }

    // Assign an existing (possibly unassigned) time point to a participant
    public async Task<bool> AssignTimePointToRaceParticipantAsync(Guid timePointId, Guid raceId, Guid participantId)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        RaceParticipantTimePoint? tp = await _db.RaceParticipantTimePoints.FindAsync(timePointId);
        if (tp is null) return false;
        tp.ParticipantID = participantId;

        RaceParticipant? affectedRP = await _db.RaceParticipants
            .Where(rp => rp.RaceID == raceId &&
                        rp.ParticipantID == participantId)
            .Include(rp => rp.RaceParticipantTimePoints)
            .Include(rp => rp.Race)
            .ThenInclude(r => r.RaceTimePoints)
            .SingleOrDefaultAsync();

        if (affectedRP is null) return false;

        uint maxCurrentTPIndex = affectedRP.RaceParticipantTimePoints.Max(rptp => rptp.RTPIndex) ?? 0;

        uint nextIndex = affectedRP.Race.RaceTimePoints.Where(rtp => rtp.Index > maxCurrentTPIndex).Min(rtp => rtp.Index);
        uint maxIndex = affectedRP.Race.RaceTimePoints.Max(rtp => rtp.Index);

        tp.RaceID = affectedRP.RaceID;
        tp.RTPIndex = nextIndex;
        tp.ParticipantID = participantId;

        bool checkForCompletion = false;

        if (nextIndex == 1)
        {
            affectedRP.StartTime = tp.TimePointUTC;
        }
        if (nextIndex == maxIndex)
        {
            affectedRP.FinishDateTimeUTC = tp.TimePointUTC;
            checkForCompletion = true;
        }

        await _db.SaveChangesAsync();

        if (checkForCompletion)
        {
            await CheckForRaceCompletionAsync(affectedRP.RaceID);
        }

        return true;
    }

    private async Task CheckForRaceCompletionAsync(Guid raceId)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        Race? race = await _db.Races.FindAsync(raceId);

        if (race is null) return;

        if (!await _db.Races.Where(r => r.Id == raceId).AllAsync(r => r.RaceParticipants.All(rp => rp.FinishDateTimeUTC != null))) return;

        race.FinishDateTimeUTC = await _db.Races.Where(r => r.Id == raceId).Select(r => r.RaceParticipants.Max(rp => rp.FinishDateTimeUTC)).SingleAsync();

        await _db.SaveChangesAsync();

    }

    public async Task<IEnumerable<RaceParticipant>> GetRacesParticipantsAsync(Guid id)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        return await _db.RaceParticipants.Where(rp => rp.RaceID == id).ToListAsync();
    }

    public async Task<IEnumerable<RaceParticipantTimePoint>?> GetUnassignedTimepointsAsync()
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        return await _db.RaceParticipantTimePoints.Where(tp => tp.RaceID == null && tp.ParticipantID == null).ToListAsync();
    }

    public async Task<IEnumerable<RaceParticipantTimePoint>> GetRaceParticipantTimePointsForRaceAsync(Guid raceId)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        return await _db.RaceParticipantTimePoints.Where(rp => rp.RaceID == raceId).ToListAsync();
    }

    public async Task<bool> SetRaceParticipantTimePointPenaltyTime(Guid timePointId, TimeSpan penaltyTime)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        RaceParticipantTimePoint? timePoint = await _db.RaceParticipantTimePoints.FindAsync(timePointId);

        if (timePoint is null) return false;

        timePoint.PenaltyTime = penaltyTime;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteRaceParticipantTimePointAsync(Guid timePointId)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        //TODO: Check ob das Rennen bearbeitbar ist also nur RaceSTatus.running. sonst return false

        var timePoint = _db.RaceParticipantTimePoints.Find(timePointId);
        if (timePoint is null) return false;

        _db.RaceParticipantTimePoints.Remove(timePoint);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CopyRaceTimePointsAsync(Guid raceIdCopyFrom, Guid raceIdCopyTo)
    {
        await CheckAndApplyMigrationsAsync();
        ///TODO: 
        /// rennen laden, prüfen ob kopierziel überschrieben werden darf (nur wenn noch nicht gestartet)
        /// existierende TimePoints des Ziels löschen, Kopien der TimePoints des copyFrom ins Ziel einfügen


        throw new NotImplementedException();
    }

    public async Task<IEnumerable<RaceTimePoint>> GetRaceTimePointsAsync(Guid raceId)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        return await _db.RaceTimePoints.Where(rtp => rtp.RaceID == raceId).ToListAsync();
    }

    public async Task<RaceTimePoint?> CreateRaceTimePointAsync(Guid raceId, string? name)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        //TODO: Check ob Race im Status Planned ist, sonst ist keine Bearbeitung erlaubt

        var existing = await _db.RaceTimePoints.Where(rtp => rtp.RaceID == raceId).OrderBy(rtp => rtp.Index).ToListAsync();

        uint newIndex = existing.Max(rtp => rtp.Index) + 1;

        RaceTimePoint newRTP = new()
        {
            DisplayName = name ?? "New",
            Index = newIndex,
            RaceID = raceId
        };

        _db.RaceTimePoints.Add(newRTP);

        await _db.SaveChangesAsync();

        return newRTP;
    }

    public async Task<bool> DeleteRaceTimePointAsync(Guid timePointId)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();

        //TODO: Check ob Race im Status Planned ist, sonst ist keine Bearbeitung erlaubt
        var timePoint = await _db.RaceTimePoints.FindAsync(timePointId);

        if (timePoint == null) return false;

        var existing = await _db.RaceTimePoints.Where(rtp => rtp.RaceID == timePoint.RaceID).OrderBy(rtp => rtp.Index).ToListAsync();

        existing.Remove(timePoint);
        _db.RaceTimePoints.Remove(timePoint);

        uint index = 1;
        foreach (var rtp in existing)
        {
            rtp.Index = index;
            index++;
        }

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateTimePointsAsync(Guid raceId, List<RaceTimePoint> timePoints)
    {
        await CheckAndApplyMigrationsAsync();
        using RaceTimerDbContext _db = await dbContextFactory.CreateDbContextAsync();
        //TODO: Check ob Race im Status Planned ist, sonst ist keine Bearbeitung erlaubt

        var existings = await _db.RaceTimePoints.Where(rtp => rtp.RaceID == raceId).OrderBy(rtp => rtp.Index).ToListAsync();

        uint index = 1;

        if (timePoints.Count != existings.Count) return false;

        foreach (RaceTimePoint changed in timePoints)
        {
            changed.Index = index;
            RaceTimePoint? existing = existings.FirstOrDefault(rtp => rtp.Id == changed.Id);

            if (existing == null) return false;

            _db.Entry(existing).CurrentValues.SetValues(changed);

            index++;
        }

        await _db.SaveChangesAsync();
        return true;
    }
}
