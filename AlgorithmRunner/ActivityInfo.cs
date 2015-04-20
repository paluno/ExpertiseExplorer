namespace AlgorithmRunner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using System.Linq;

    internal class ActivityInfo : ReviewInfo
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

        #region Factories
        private ActivityInfo(string inputLine)
        {
            string line = inputLine.ToLower();
            var fields = line.Split(';');

            BugId = int.Parse(fields[0]);
            ActivityId = int.Parse(fields[1]);
            Author = fields[2];
            What = fields[4];
            Removed = fields[5];
            Added = fields[6];

            SetDateTimeFromUnixTime(long.Parse(fields[3]));
        }

        public static IEnumerable<ActivityInfo> GetActivityInfoFromFile(string pathToInputFile, string pathToAttachments)
        {
            Dictionary<int, List<string>> attachments = new Dictionary<int, List<string>>();
            var attachmentLines = File.ReadAllLines(pathToAttachments);
            foreach (var attachmentLine in attachmentLines)
            {
                var attachmentId = int.Parse(attachmentLine.Split(';')[1]);
                attachments.Add(attachmentId, attachmentLine.Split(';')[2].Split(',').Distinct().ToList());
            }


            var input = new StreamReader(pathToInputFile);
            var result = new List<ActivityInfo>();
            Debug.WriteLine("Starting ActivityInfo parsing at: " + DateTime.Now);
            try
            {
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    ActivityInfo activityInfo = new ActivityInfo(line);
                    int? attachmentId = activityInfo.GetAttachmentId();

                    if (attachmentId != null)
                    {
                        activityInfo.Filenames = attachments[(int)attachmentId];
                    }

                    result.Add(activityInfo);
                }
            }
            finally
            {
                input.Close();
            }




            Debug.WriteLine("Finished ActivityInfo parsing at: " + DateTime.Now);

            return result;
        }
        #endregion Factories

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
