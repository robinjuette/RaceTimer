using RaceTimer.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RaceTimerApp.Shared.Models
{
    public class RankingEntry(RaceParticipant raceParticipant,
        Dictionary<uint, TimeSpan?> splitTimes,
        Dictionary<uint, TimeSpan?> penaltyTimes)
    {
        public RaceParticipant RaceParticipant => raceParticipant;
        public Dictionary<uint, TimeSpan?> SplitTimes => splitTimes;
        public Dictionary<uint, TimeSpan?> PenaltyTimes => penaltyTimes;
        public uint Position { get; set; }

        public decimal Progress
        {
            get
            {
                return Convert.ToDecimal(SplitTimes.Where(kvp => kvp.Value.HasValue).Count()) / SplitTimes.Count;
            }
        }
        public TimeSpan CurrentPenaltyTime
        {
            get
            {
                return new(PenaltyTimes.Select(rptp => rptp.Value?.Ticks ?? 0).Sum());
            }
        }
        public bool AllPenaltyTimesEntered
        {
            get
            {
                return PenaltyTimes.All(kvp => kvp.Value.HasValue);
            }
        }

        public TimeSpan RunTime
        {
            get
            {
                if (RaceParticipant.StartTime.HasValue)
                {
                    DateTime curEnd = RaceParticipant.FinishDateTimeUTC ?? DateTime.UtcNow;

                    return (curEnd - RaceParticipant.StartTime.Value) + CurrentPenaltyTime;
                }

                return TimeSpan.Zero;
            }
        }

        public bool CompletelyFinished => Progress == 1m && AllPenaltyTimesEntered;
    }
}
