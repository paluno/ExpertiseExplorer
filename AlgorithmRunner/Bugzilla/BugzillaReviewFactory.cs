using ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker;
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

            return Merge<IssueTrackerEvent>(attachmentList, reviewList, (patch, review) => patch.When <= review.When);
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

        protected override void PrefilterRawInput(string pathToRawInputFile)
        {
            string filteredInput;
            using (StreamReader input = new StreamReader(pathToRawInputFile))
                filteredInput = BugzillaReview.ParseAndFilterInput(input);

            File.WriteAllText(InputFilePath, filteredInput);
        }

        #region Code from svick posted on https://stackoverflow.com/questions/7717871/how-to-perform-merge-sort-using-linq
        /// <summary>
        /// Merge-Sort-style ordered union of two sequences
        /// </summary>
        static IEnumerable<T> Merge<T>(IEnumerable<T> first,
                               IEnumerable<T> second,
                               Func<T, T, bool> predicate)
        {
            // validation ommited

            using (var firstEnumerator = first.GetEnumerator())
            using (var secondEnumerator = second.GetEnumerator())
            {
                bool firstCond = firstEnumerator.MoveNext();
                bool secondCond = secondEnumerator.MoveNext();

                while (firstCond && secondCond)
                {
                    if (predicate(firstEnumerator.Current, secondEnumerator.Current))
                    {
                        yield return firstEnumerator.Current;
                        firstCond = firstEnumerator.MoveNext();
                    }
                    else
                    {
                        yield return secondEnumerator.Current;
                        secondCond = secondEnumerator.MoveNext();
                    }
                }

                while (firstCond)
                {
                    yield return firstEnumerator.Current;
                    firstCond = firstEnumerator.MoveNext();
                }

                while (secondCond)
                {
                    yield return secondEnumerator.Current;
                    secondCond = secondEnumerator.MoveNext();
                }
            }
        }
        #endregion Code from svick posted on https://stackoverflow.com/questions/7717871/how-to-perform-merge-sort-using-linq
    }
}
