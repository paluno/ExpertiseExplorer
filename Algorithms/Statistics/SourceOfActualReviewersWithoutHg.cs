using ExpertiseExplorer.ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms.Statistics
{
    class SourceOfActualReviewersWithoutHg : SourceOfActualReviewers
    {
        public override string Postfix
        {
            get { return "_wo_hg"; }
        }

        /// <summary>
        /// filters Mozilla's original import "author" hg@mozilla.com
        /// </summary>
        /// <returns>Ids of Bugs that do not contain any reference to hg@mozilla.com</returns>
        public override IEnumerable<int> findBugsInDatabase()
        {
            using (var context = new ExpertiseDBEntities())
            {
                        // TODO: First find ID of hg@mozilla.com, then filter
                return context.Bugs
                        // find all bugs where no algorithm suggests hg@mozilla.com
                    .Where(bug => bug.RepositoryId == RepositoryId && bug.ComputedReviewers.All(cr => cr.Expert1.Name != "hg@mozilla.com" && cr.Expert2.Name != "hg@mozilla.com" && cr.Expert3.Name != "hg@mozilla.com" && cr.Expert4.Name != "hg@mozilla.com" && cr.Expert5.Name != "hg@mozilla.com"))
                    .Select(bug => bug.BugId)
                    .ToList();
            }
        }

        public SourceOfActualReviewersWithoutHg(int repositoryId)
            : base(repositoryId)
        {
        }
    }
}
