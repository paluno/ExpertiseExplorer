using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorerCommon
{
    public static class UnixTime
    {
        public static DateTime UnixTime2PDTDateTime(this long unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(unixTime)
                .Subtract(new TimeSpan(0, 7, 0, 0)); // From Utc to PDT
        }

        public static long PDTDateTime2unixTime(this DateTime pdtTime)
        {
            return Convert.ToInt64(
                pdtTime.AddHours(7)  // to UTC
                    .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                    .TotalSeconds
                );
        }
    }
}
