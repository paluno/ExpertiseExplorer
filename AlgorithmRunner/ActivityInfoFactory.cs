using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner
{
    class ActivityInfoFactory : ReviewInfoFactory
    {
        public string ActivityLogPath { get; private set; }
        public string AttachmentPath { get; private set; }

        public ActivityInfoFactory(string pathToActivityLog, string pathToAttachments)
        {
            ActivityLogPath = pathToActivityLog;
            AttachmentPath = pathToAttachments;
        }

        /// <summary>
        /// parses, filters and orders the bugzilla activity log respecting the specifics of Bugzilla
        /// </summary>
        public void PrepareInputFromMozillaLog(string pathToOutputFile, bool overwrite = false)
        {
            if (!overwrite && File.Exists(pathToOutputFile))
                return;

            var input = new StreamReader(ActivityLogPath);
            string filteredInput;
            Debug.WriteLine("Starting parsing at: " + DateTime.Now);
            try
            {
                filteredInput = ActivityInfo.ParseAndFilterInput(input);
            }
            finally
            {
                input.Close();
            }

            Debug.WriteLine("Finished parsing at: " + DateTime.Now);

            File.WriteAllText(pathToOutputFile, filteredInput);

            Debug.WriteLine("Starting ordering at: " + DateTime.Now);

            ActivityInfoFactory factory = new ActivityInfoFactory(pathToOutputFile, AttachmentPath);
            IEnumerable<ActivityInfo> list = (IEnumerable<ActivityInfo>)factory.parseReviewInfos();

            // ordering of & another filter pass on the activities
            var mercurialTransferDate = new DateTime(2007, 3, 22, 18, 29, 0); // date of Mozilla's move to hg
            var endOfHgDump = new DateTime(2013, 3, 8, 16, 15, 44); // last date of the hg dump
            var timeOrder = new List<long>();
            var activityLookupTable = new Dictionary<long, List<ActivityInfo>>();
            foreach (var activityInfo in list)
            {
                // filter if not review
                if (!activityInfo.IsReview)
                    continue;

                // filter if not in examined window of time
                if (activityInfo.When < mercurialTransferDate || activityInfo.When > endOfHgDump)
                    continue;

                IList<String> involvedFiles = activityInfo.Filenames;

                // filter if there are no files
                if (!involvedFiles.Any())
                    continue;

                // filter if there is only one file with no name 
                if (involvedFiles.Count == 1 && involvedFiles[0] == string.Empty)
                    continue;

                var key = activityInfo.UnixTime;
                timeOrder.Add(key);

                if (!activityLookupTable.ContainsKey(key))
                    activityLookupTable.Add(key, new List<ActivityInfo>());

                activityLookupTable[key].Add(activityInfo);
            }

            // needed b/c bugzilla logs are ordered according to bugid, not datetime
            timeOrder = timeOrder.Distinct().ToList();
            timeOrder.Sort();
            var sb = new StringBuilder();
            foreach (var activityInfo in timeOrder.SelectMany(unixTime => activityLookupTable[unixTime]))
            {
                sb.AppendLine(activityInfo.ToString());
            }

            Debug.WriteLine("Finished ordering at: " + DateTime.Now);

            File.WriteAllText(pathToOutputFile, sb.ToString());
        }

        public override IEnumerable<ReviewInfo> parseReviewInfos()
        {
            return GetActivityInfoFromFile(ActivityLogPath, AttachmentPath);
        }

        private static IEnumerable<ActivityInfo> GetActivityInfoFromFile(string pathToInputFile, string pathToAttachments)
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
    }
}
