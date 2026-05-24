using System;
using System.Collections.Generic;
using System.Text;

namespace RaceTimer.Shared.Models
{
    public class RaceParticipantTimePoint
    {
        public Guid Id { get; set; }
        public DateTime TimePointUTC { get; set; }
        public TimeSpan? PenaltyTime { get; set; }
        public Guid? RaceID { get; set; }
        public Race? Race { get; set; }
        public Guid? ParticipantID { get; set; }
        public Participant? Participant { get; set; }
        public int? RTPIndex { get; set; }
        public RaceTimePoint? RaceTimePoint { get; set; }
    }
}
