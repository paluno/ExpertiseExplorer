namespace AlgorithmRunner.Bugzilla
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Linq;
    using AlgorithmRunner.AbstractIssueTracker;

    internal class BugzillaReview : ReviewInfo
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

        public override string ChangeId
        {
            get
            {
                return BugId.ToString();
            }
            set
            {
                BugId = int.Parse(value);
            }
        }

        public override int ActivityId { get; set; }

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

        internal BugzillaReview(string inputLine)
        {
            string line = inputLine.ToLower();
            var fields = line.Split(';');

            BugId = int.Parse(fields[0]);
            ActivityId = int.Parse(fields[1]);
            Reviewer = fields[2];
            What = fields[4];
            Removed = fields[5];
            Added = fields[6];

            SetDateTimeFromUnixTime(long.Parse(fields[3]));
        }

        private readonly DateTime mercurialTransferDate = new DateTime(2007, 3, 22, 18, 29, 0); // date of Mozilla's move to hg
        private readonly DateTime endOfHgDump = new DateTime(2013, 3, 8, 16, 15, 44); // last date of the hg dump

        public override bool isValid()
        {
            // filter if not review
            if (IsReview)
                return false;

            // filter if not in examined window of time
            if (When < mercurialTransferDate || When > endOfHgDump)
                return false;

            IList<String> involvedFiles = Filenames;

            // filter if there are no files
            if (!involvedFiles.Any())
                return false;

            // filter if there is only one file with no name 
            if (involvedFiles.Count == 1 && involvedFiles[0] == string.Empty)
                return false;

            return true;
        }

        public static DateTime UnixTime2PDTDateTime(long unixTime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(unixTime)
                .Subtract(new TimeSpan(0, 7, 0, 0)); // From Utc to PDT
        }

        public static long PDTDateTime2unixTime(DateTime pdtTime)
        {
            return Convert.ToInt64(
                pdtTime.AddHours(7)  // to UTC
                    .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                    .TotalSeconds
                );
        }

        public void SetDateTimeFromUnixTime(long unixTime)
        {
            UnixTime = unixTime;
            When = UnixTime2PDTDateTime(UnixTime);
        }

        public override string ToString()
        {
            return base.ToString() + ";" + What + ";" + Removed + ";" + Added;
        }

        /// <summary>
        /// removes supperreview strings and afterwards returns only lines that stll contain 'review+' or 'review-'
        /// </summary>
        internal static string ParseAndFilterInput(TextReader input)
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
