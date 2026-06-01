using RaceTimer.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace RaceTimerApp.Shared.Models
{
    public class ResultEntry
    {
        public required RankingEntry RankingEntry { get; set; }
        public required Dictionary<uint, ResultEntryPoint> ResultEntryPoints { get; set; }
        public required TimeSpan DrivingTimeSpan { get; set; }
        public required TimeSpan ShootingTimeSpan { get; set; }
        public required TimeSpan PenaltyTimeSpan { get; set; }
        public TimeSpan TotalTimeSpan => DrivingTimeSpan + ShootingTimeSpan + PenaltyTimeSpan;
    }

    public class ResultEntryPoint
    {
        public required RaceParticipantTimePoint RaceParticipantTimePoint { get; set; }
        public required TimeSpan SplitTimeSpan { get; set; }
        public required TimeSpan? PenaltyTimeSpan { get; set; }
        public required TimeSpan CummulativeTimeSpan { get; set; }
    }
}
