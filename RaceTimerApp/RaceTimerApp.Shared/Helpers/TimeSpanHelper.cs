using System;
using System.Collections.Generic;
using System.Text;

namespace RaceTimerApp.Shared.Helpers
{
    public static class TimeSpanHelper
    {
        public static string FormatShort(this TimeSpan? timeSpan)
        {
            
            if (timeSpan == null)
            {
                return " - ";
            }

            return timeSpan.Value.FormatShort();
        }
        public static string FormatShort(this TimeSpan timeSpan)
        {
            string format = @"mm\:ss";
            if (timeSpan.TotalHours > 1)
            {
                format = @"hh\:" + format;

                if (timeSpan.TotalDays > 1)
                {
                    format = @"d\." + format;
                }
            }

            return timeSpan.ToString(format);
            
        }
    }
}
