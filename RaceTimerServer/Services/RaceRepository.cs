using Microsoft.EntityFrameworkCore;
using RaceTimer.Shared.Models;
using RaceTimerServer.Data;

namespace RaceTimerServer.Services;

public class RaceRepository
{
    private readonly RaceTimerDbContext _db;

    public RaceRepository(RaceTimerDbContext db)
    {
        _db = db;
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
    }

    public async Task UpdateAsync(Race race)
    {
        var existing = await _db.Races.FindAsync(race.Id);
        if (existing is null) return;
        _db.Entry(existing).CurrentValues.SetValues(race);
        // for related collections you may need to handle updates explicitly
        await _db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Guid id)
    {
        var existing = await _db.Races.FindAsync(id);
        if (existing is null) return;
        _db.Races.Remove(existing);
        await _db.SaveChangesAsync();
    }
}
