namespace AlgorithmRunner
{
    using System;

    internal class ActivityInfo
    {
        public int BugId { get; set; }

        public int ActivityId { get; set; }

        public string Author { get; set; }

        public DateTime When { get; set; }

        public string What { get; set; }

        public string Removed { get; set; }

        public string Added { get; set; }

        public long UnixTime { get; private set; }

        public void SetDateTimeFromUnixTime(long unixTime)
        {
            UnixTime = unixTime;
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            epoch = epoch.AddSeconds(unixTime);
            When = epoch.Subtract(new TimeSpan(0, 7, 0, 0)); // From Utc to PDT
        }

        public override string ToString()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var originalUtc = When.Add(new TimeSpan(0, 7, 0, 0));
            var secondspassed = Convert.ToInt64((originalUtc - epoch).TotalSeconds);
            return BugId + ";" + ActivityId + ";" + Author + ";" + secondspassed + ";" + What + ";" + Removed + ";" + Added;
        }
    }
}
