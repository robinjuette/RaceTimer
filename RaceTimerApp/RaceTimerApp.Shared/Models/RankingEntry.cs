using BlazorBootstrap;
using RaceTimer.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RaceTimerApp.Shared.Models
{
    public class RankingEntry(RaceParticipant raceParticipant,
        Dictionary<uint, TimeSpan?> splitTimes,
        Dictionary<uint, TimeSpan?> penaltyTimes,
        List<RaceParticipantTimePoint> raceParticipantTimePoints)
    {
        public RaceParticipant RaceParticipant => raceParticipant;
        public Dictionary<uint, TimeSpan?> SplitTimes => splitTimes;
        public Dictionary<uint, TimeSpan?> PenaltyTimes => penaltyTimes;
        public List<RaceParticipantTimePoint> RaceParticipantTimePoints => raceParticipantTimePoints;
        public uint Position { get; set; }

        public decimal Progress
        {
            get
            {
                decimal withValue = SplitTimes.Where(kvp => kvp.Value.HasValue).Count() - 1;
                decimal total = SplitTimes.Count-1;
                return withValue / total;
            }
        }
        public double ProgressPercent
        {
            get
            {
                return Convert.ToDouble(Progress * 100);
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
                return new(SplitTimes.Values.Sum(v => v?.Ticks ?? 0) + PenaltyTimes.Values.Sum(v => v?.Ticks ?? 0));
            }
        }

        public bool CompletelyFinished => Progress == 1m && AllPenaltyTimesEntered;
    }
}
