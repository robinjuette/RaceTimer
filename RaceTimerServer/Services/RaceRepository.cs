using RaceTimer.Shared.Models;

namespace RaceTimerServer.Services;

public class RaceRepository
{
    private readonly List<Race> _races = new();

    public IEnumerable<Race> GetAll() => _races;

    public Race? Get(Guid id) => _races.FirstOrDefault(r => r.Id == id);

    public void Add(Race race)
    {
        if (race.Id == Guid.Empty) race.Id = Guid.NewGuid();
        _races.Add(race);
    }

    public void Update(Race race)
    {
        var existing = Get(race.Id);
        if (existing is null) return;
        existing.Name = race.Name;
        existing.StartTime = race.StartTime;
        existing.Participants = race.Participants;
    }

    public void Remove(Guid id) => _races.RemoveAll(r => r.Id == id);
}
