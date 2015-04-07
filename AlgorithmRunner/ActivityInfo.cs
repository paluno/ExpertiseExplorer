namespace AlgorithmRunner
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    internal class ActivityInfo
    {
        #region Constant Strings from Bugzilla Logs

        private const string FLAGS = "flags";
        private const string REVIEWPLUS = "review+";
        private const string REVIEWMINUS = "review-";
        private const string SUPERREVIEWREQUEST = "superreview?";
        private const string SUPERREVIEWPLUS = "superreview+";
        private const string SUPERREVIEWMINUS = "superreview-";
        private const string ATTACHMENTIDENTIFIER = "attachment #";

        #endregion // Constant Strings from Bugzilla Logs

        public int BugId { get; set; }

        public int ActivityId { get; set; }

        public string Author { get; set; }

        public DateTime When { get; set; }

        public string What { get; set; }

        public string Removed { get; set; }

        public string Added { get; set; }

        public long UnixTime { get; private set; }

        public bool IsReview
        {
            get
            {
                return Added.Contains(REVIEWPLUS) || Added.Contains(REVIEWMINUS);
            }
        }

        public int? GetAttachmentId()
        {
            if (!What.Contains(ATTACHMENTIDENTIFIER))
                return null;

            return int.Parse(
                    What.Replace(ATTACHMENTIDENTIFIER, string.Empty)
                        .Replace(FLAGS, string.Empty)
                        .Trim()
                );
        }

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

        /// <summary>
        /// removes supperreview strings and afterwards returns only lines that stll contain 'review+' or 'review-'
        /// </summary>
        public static string ParseAndFilterInput(TextReader input)
        {
            StringBuilder result = new StringBuilder();
            string line;
            while ((line = input.ReadLine()) != null)
            {
                line = line.ToLower();
                line = line.Replace(SUPERREVIEWREQUEST, string.Empty);
                line = line.Replace(SUPERREVIEWPLUS, string.Empty);
                line = line.Replace(SUPERREVIEWMINUS, string.Empty);
                if ((line.Contains(REVIEWMINUS) || line.Contains(REVIEWPLUS)) && line.Contains(FLAGS))
                    result.AppendLine(line);
            }

            return result.ToString();
        }
    }
}
