using RaceTimer.Shared.Models;
using RaceTimer.Shared.Services;
using RaceTimerApp.Shared.Models;

namespace RaceTimerApp.Shared.Services;

/// <summary>
/// Service für Sortierung und Ranglistenberechnung
/// </summary>
public class RankingService(IRaceRepository raceRepository)
{

    public async Task<List<RankingEntry>> GetRankingsAsync(Guid raceId)
    {
        IEnumerable<RaceParticipant>? rps = await raceRepository.GetRacesParticipantsAsync(raceId);
        IEnumerable<RaceTimePoint>? rtps = await raceRepository.GetRaceTimePointsAsync(raceId);
        List<RaceParticipantTimePoint>? rptps = (await raceRepository.GetRaceParticipantTimePointsForRaceAsync(raceId))?.ToList();

        if(rps == null || rtps == null || rptps == null)
        {
            return [];
        }

        Dictionary<RaceParticipant, RankingEntry> rankingEntriesDict = new();

        foreach(RaceParticipant rp in rps)
        {
            rankingEntriesDict[rp] = GetRankingEntry(rp, rtps, rptps);
        }

        List<RankingEntry> rankingEntries = rankingEntriesDict.Values.Where(re => re.Progress == 1).OrderBy(re => re.RunTime).ToList();

        List<RaceTimePoint> raceTimePointsDesc = rtps.OrderByDescending(rtp => rtp.Index).ToList();
        //Die fertigen Entries wollen wir ja nicht nochmal mit reinpacken
        raceTimePointsDesc.RemoveAt(0);

        TimeSpan SumSplitsAndPenaltiesToIndex(RankingEntry rankingEntry, uint maxIndex)
        {
            return new(rankingEntry
                .SplitTimes
                .Where(kvp => kvp.Key <= maxIndex && kvp.Value.HasValue)
                .Sum(kvp => kvp.Value.Value.Ticks)
                +
                rankingEntry.PenaltyTimes
                .Where(kvp => kvp.Key <= maxIndex && kvp.Value.HasValue)
                .Sum(kvp => kvp.Value.Value.Ticks)
                );
        }

        foreach(RaceTimePoint raceTimePoint in raceTimePointsDesc)
        {
            IEnumerable<RankingEntry> progressedToHere = rankingEntriesDict.Values.Where(re => re.SplitTimes.Where(kvp => kvp.Value.HasValue).Max(kvp => kvp.Key) == raceTimePoint.Index);

            foreach(RankingEntry curProgressed in progressedToHere)
            {
                TimeSpan progressTimeSpan = SumSplitsAndPenaltiesToIndex(curProgressed, raceTimePoint.Index);

                bool inserted = false;

                for(int i = 0; i < rankingEntries.Count && !inserted; i++)
                {
                    TimeSpan curEntriesProgressTimeSpan = SumSplitsAndPenaltiesToIndex(rankingEntries[i], raceTimePoint.Index);

                    if(curEntriesProgressTimeSpan > progressTimeSpan)
                    {
                        rankingEntries.Insert(i, curProgressed);
                        inserted = true;
                    }
                }

                if (!inserted)
                {
                    rankingEntries.Add(curProgressed);
                }
            }
        }


        for (int i = 0; i < rankingEntries.Count; i++)
        {
            rankingEntries[i].Position = (uint)i+1;
        }

        return rankingEntries;
    }

    private RankingEntry GetRankingEntry(RaceParticipant raceParticipant, IEnumerable<RaceTimePoint> raceTimePoints, IEnumerable<RaceParticipantTimePoint> raceParticipantTimePoints)
    {
        List<RaceParticipantTimePoint> tps = raceParticipantTimePoints.Where(rptp => rptp.ParticipantID == raceParticipant.ParticipantID).ToList();

        Dictionary<uint, TimeSpan?> splitTimes = new();
        splitTimes[1] = TimeSpan.Zero;
        DateTime? prevDateTimeUTC = raceParticipant.StartTime;

        for(uint i = 2; i <= raceTimePoints.Max(rtp => rtp.Index); i++)
        {
            DateTime? curDateTimeUTC = tps.FirstOrDefault(rptp => rptp.RTPIndex == i)?.TimePointUTC;
            if(prevDateTimeUTC.HasValue && curDateTimeUTC.HasValue)
            {
                splitTimes[i] = curDateTimeUTC.Value - prevDateTimeUTC.Value;
            }
            else
            {
                splitTimes[i] = null;
            }
            prevDateTimeUTC = curDateTimeUTC;
        }

        return new(raceParticipant, 
            splitTimes, 
            raceTimePoints.ToDictionary(rtp => rtp.Index, rtp => rtp.HasPenaltyTime ? tps.FirstOrDefault(rptp => rptp.RTPIndex == rtp.Index)?.PenaltyTime : TimeSpan.Zero));
    }

}
