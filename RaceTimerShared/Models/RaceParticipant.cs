using System;
using System.Collections.Generic;
using System.Text;

namespace RaceTimer.Shared.Models
{
    public class RaceParticipant
    {
        public Guid ParticipantID { get; set; }
        public Participant Participant { get; set; }
        public Guid RaceID { get; set; }
        public Race Race { get; set; }
        public DateTime? StartTime { get; set; }
    }
}
