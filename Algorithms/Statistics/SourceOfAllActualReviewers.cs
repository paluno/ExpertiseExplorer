using ExpertiseExplorer.ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms.Statistics
{
    class SourceOfAllActualReviewers : SourceOfActualReviewers
    {
        public override IEnumerable<int> findBugsInDatabase()
        {
            using (var context = new ExpertiseDBEntities())
                return context.Bugs.Where(bug => bug.RepositoryId == RepositoryId).Select(bug => bug.BugId).ToList();
        }

        public SourceOfAllActualReviewers(int repositoryId)
            : base(repositoryId)
        {
        }

        public override string Postfix
        {
            get { return string.Empty; }
        }
    }
}
