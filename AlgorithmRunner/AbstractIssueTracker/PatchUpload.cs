using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker
{
    /// <summary>
    /// Represents the upload of a patch (Bugzilla) or a commit (Gerrit); in all cases, this is the moment
    /// that the core developers may become aware of the modification that an author submits.
    /// </summary>
    abstract class PatchUpload : IssueTrackerEvent
    {
    }
}
