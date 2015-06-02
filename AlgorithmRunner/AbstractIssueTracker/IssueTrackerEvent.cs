using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner.AbstractIssueTracker
{
    abstract class IssueTrackerEvent
    {
        public virtual string ChangeId { get; set; }

        public virtual DateTime When { get; set; }

        public virtual IList<String> Filenames { get; set; }

        public abstract bool isValid();
    }
}
