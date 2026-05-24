using System;
using System.Collections.Generic;
using System.Text;

namespace RaceTimer.Shared.Models
{
    public class RaceTimePoint
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public uint Index { get; set; }
        public Guid RaceID { get; set; }
        public Race Race { get; set; }
    }
}
