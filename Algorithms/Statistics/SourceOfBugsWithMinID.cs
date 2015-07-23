using ExpertiseExplorer.ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms.Statistics
{
    // REVISIT: Should be a Decorator
    public class SourceOfBugsWithMinID : AbstractSourceOfBugs
    {
        public int MinimumBugID { get; }

        public override string Postfix => "_minBug_" + MinimumBugID;

        public SourceOfBugsWithMinID(int repositoryId, int minimumBugID)
            : base(repositoryId)
        {
            this.MinimumBugID = minimumBugID;
        }

        protected override IEnumerable<int> findBugsInDatabase()
        {
            using (var context = new ExpertiseDBEntities())
                return context.Bugs
                    .Where(bug => bug.RepositoryId == RepositoryId && bug.BugId >= MinimumBugID)
                    .Select(bug => bug.BugId).ToList();
        }
    }
}
