using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorerCommon
{
    public static class UnixTime
    {
        public static readonly DateTime UnixStartOfTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime UnixTime2UTCDateTime(this long unixTime)
        {
            return UnixStartOfTime.AddSeconds(unixTime);
        }

        public static long UTCDateTime2unixTime(this DateTime utcTime)
        {
            return Convert.ToInt64(
                utcTime.Subtract(UnixStartOfTime).TotalSeconds
                );
        }
    }
}
