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
                long recordedTicks = SplitTimes.Values.Sum(v => v?.Ticks ?? 0) + PenaltyTimes.Values.Sum(v => v?.Ticks ?? 0);
                if (!RaceParticipant.FinishDateTimeUTC.HasValue && RaceParticipant.Race?.RaceStatus == RaceStatus.Running)
                {
                    DateTime? lastTimePoint = RaceParticipantTimePoints
                        .OrderByDescending(tp => tp.RTPIndex)
                        .FirstOrDefault()?.GetEffectiveTimePoint() ?? RaceParticipant.StartTime;

                    if (lastTimePoint.HasValue)
                    {
                        recordedTicks += Math.Max(0, (DateTime.UtcNow - lastTimePoint.Value).Ticks);
                    }
                }

                return new(recordedTicks);
            }
        }

        public bool CompletelyFinished => Progress == 1m && AllPenaltyTimesEntered;
    }
}
