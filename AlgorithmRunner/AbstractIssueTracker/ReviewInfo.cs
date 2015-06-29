using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpertiseExplorer.Common;

namespace ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker
{
    abstract class ReviewInfo : IssueTrackerEvent
    {
        public virtual int ActivityId { get; set; }

        public virtual string Reviewer { get; set; }

        public override string ToString()
        {
            return ChangeId + ";" + ActivityId + ";" + Reviewer + ";" + When.UTCDateTime2unixTime();
        }
    }
}
