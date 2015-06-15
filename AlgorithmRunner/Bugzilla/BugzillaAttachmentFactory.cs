using AlgorithmRunner.AbstractIssueTracker;
using ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner.Bugzilla
{
    class BugzillaAttachmentFactory : IssueTrackerEventFactory
    {
        /// <summary>
        /// If set, only attachments in the list will be returned.
        /// As a consequence, attachments not in the list will be deleted when PrepareInput is called.
        /// </summary>
        internal Func<BugzillaAttachmentInfo, bool> IncludeAttachmentsFilter { get; set; }

        public BugzillaAttachmentFactory(string pathToAttachments)
            : base(pathToAttachments)
        {
            IncludeAttachmentsFilter = (dummy) => true;
        }

        public override IEnumerable<IssueTrackerEvent> parseIssueTrackerEvents()
        {
            return parseIssueTrackerEvents(InputFilePath);
        }

        public IEnumerable<BugzillaAttachmentInfo> parseIssueTrackerEvents(string attachmentPath)
        {
            IEnumerable<BugzillaAttachmentInfo> allBugs = File.ReadAllLines(attachmentPath)
                .Select(attachmentCSVLine => new BugzillaAttachmentInfo(attachmentCSVLine));

            return allBugs.Where(IncludeAttachmentsFilter);
        }

        /// <summary>
        /// Reads the missing upload dates of all attachments from a copy of the Bugzilla DB and saves the result as InputFilePath
        /// </summary>
        /// <param name="pathToRawInputFile">Attachment data without upload dates</param>
        protected override void PrefilterRawInput(string pathToRawInputFile)
        {
            IEnumerable<BugzillaAttachmentInfo> rawAttachments = parseIssueTrackerEvents(pathToRawInputFile).ToList();

            using (ExpertiseDBEntities repository = new ExpertiseDBEntities())
            {
                foreach (BugzillaAttachmentInfo bai in rawAttachments.Where(bai => DateTime.MinValue == bai.When))
                    bai.When = repository.Database.SqlQuery<DateTime>("SELECT creation_ts FROM attachments WHERE attach_id={0}",  // this is a table directly from the Bugzilla Database
                        bai.AttachmentId  
                    ).SingleOrDefault();
            }

            File.WriteAllLines(InputFilePath,
                rawAttachments.Select(bai => bai.ToString())
                );
        }
    }
}
