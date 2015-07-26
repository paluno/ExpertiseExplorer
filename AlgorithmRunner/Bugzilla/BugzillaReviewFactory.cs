using ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker;
using ExpertiseExplorer.ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.AlgorithmRunner.Bugzilla
{
    class BugzillaReviewFactory : IssueTrackerEventFactory
    {
        protected BugzillaAttachmentFactory AttachmentFactory { get; private set; }

        /// <summary>
        /// Attachments without date
        /// </summary>
        private string PathToRawAttachments { get; set; }

        public BugzillaReviewFactory(string pathToActivityLog, BugzillaAttachmentFactory attachments)
            : base(pathToActivityLog)
        {
            AttachmentFactory = attachments;
        }

        public void filterForUsedAttachmentsAndPersist()
        {
            parseIssueTrackerEvents();  // as a side effect, there will be a filter for valid attachments
            AttachmentFactory.PrepareInput(AttachmentFactory.InputFilePath, true);
        }

        public override IEnumerable<IssueTrackerEvent> parseIssueTrackerEvents()
        {
            // First get the patch uploads
            IEnumerable<BugzillaAttachmentInfo> attachmentList = (IEnumerable<BugzillaAttachmentInfo>)AttachmentFactory.parseIssueTrackerEvents();

            // Lookup table to add file names to BugzillaReviews
            Dictionary<UInt64, IList<string>> dictAttachments = attachmentList.ToDictionary(bai => bai.AttachmentId, bai => bai.Filenames);

            // Second, get reviews
            IEnumerable<BugzillaReview> reviewList = GetActivityInfoFromFile(InputFilePath, dictAttachments);

            HashSet<int> setOfAllUsedBugIds = new HashSet<int>(reviewList.Select(review => review.BugId));
            AttachmentFactory.IncludeAttachmentsFilter = (attachment) => setOfAllUsedBugIds.Contains(attachment.BugId);
            attachmentList = (IEnumerable<BugzillaAttachmentInfo>)AttachmentFactory.parseIssueTrackerEvents();  // re-read the list

            return MergeUtils.Merge<IssueTrackerEvent>(attachmentList, reviewList, (patch, review) => patch.When <= review.When);
        }

        private static IEnumerable<BugzillaReview> GetActivityInfoFromFile(string pathToInputFile, Dictionary<UInt64, IList<string>> attachments)
        {
            var input = new StreamReader(pathToInputFile);
            var result = new List<BugzillaReview>();
            Debug.WriteLine("Starting ActivityInfo parsing at: " + DateTime.Now);
            try
            {
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    BugzillaReview activityInfo = new BugzillaReview(line);
                    UInt64? attachmentId = activityInfo.GetAttachmentId();

                    if (attachmentId != null)
                    {
                        activityInfo.Filenames = attachments[(UInt64)attachmentId];
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

        protected override IEnumerable<IssueTrackerEvent> PrefilterRawInput(string pathToRawInputFile)
        {
            string filteredInput;
            using (StreamReader input = new StreamReader(pathToRawInputFile))
                filteredInput = BugzillaReview.ParseAndFilterInput(input);

            File.WriteAllText(InputFilePath, filteredInput);

            // check whether all names are complete. If not, load complete names from Bugzilla DB
            IEnumerable<BugzillaReview> rawReviews = parseIssueTrackerEvents().OfType<BugzillaReview>();
            TimeZoneInfo pacificTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");    // Bugzilla stores times in PST

            using (ExpertiseDBEntities repository = new ExpertiseDBEntities())
            {
                foreach (BugzillaReview br in rawReviews.Where(br => br.GetAttachmentId().HasValue && !br.Reviewer.Contains('@')))
                    br.Reviewer =
                        repository.Database.SqlQuery<string>("SELECT login_name FROM profiles p INNER JOIN bugs_activity ba ON p.userid=ba.who WHERE attach_id={0} AND bug_id={1} AND bug_when={2} AND fieldid=69",  // these are tables directly from the Bugzilla Database. fieldid 69 are Flags
                            br.GetAttachmentId(),
                            br.BugId,
                            TimeZoneInfo.ConvertTimeFromUtc(br.When, pacificTimeZone)
                        ).SingleOrDefault() ?? br.Reviewer;
            }

            return rawReviews;
        }
    }
}
