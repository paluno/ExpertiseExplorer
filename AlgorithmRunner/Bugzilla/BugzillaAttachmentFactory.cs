using AlgorithmRunner.AbstractIssueTracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner.Bugzilla
{
    class BugzillaAttachmentFactory : IssueTrackerEventFactory
    {
        public BugzillaAttachmentFactory(string pathToAttachments)
            : base(pathToAttachments)
        {
        }

        public override IEnumerable<IssueTrackerEvent> parseIssueTrackerEvents()
        {
            throw new NotImplementedException();
        }
    }
}
