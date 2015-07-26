using ExpertiseExplorer.ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms.Statistics
{
    class SourceOfBugsWithoutSpecificReviewer : AbstractSourceOfBugs
    {
        public override string Postfix
        {
            get { return "_wo_hg"; }
        }

        public string FilteredAuthor { get; }

        /// <summary>
        /// filters Mozilla's original import "author" hg@mozilla.com
        /// </summary>
        /// <returns>Ids of Bugs that do not contain any reference to hg@mozilla.com</returns>
        protected override IEnumerable<int> findBugsInDatabase()
        {
            using (var context = new ExpertiseDBEntities())
            {
                // TODO: First find ID of FilteredAuthor, then filter
                return context.Bugs
                        // find all bugs where no algorithm suggests FilteredAuthor
                    .Where(bug => bug.RepositoryId == RepositoryId && bug.ComputedReviewers.All(cr => cr.Expert1.Name != FilteredAuthor && cr.Expert2.Name != FilteredAuthor && cr.Expert3.Name != FilteredAuthor && cr.Expert4.Name != FilteredAuthor && cr.Expert5.Name != FilteredAuthor))
                    .Select(bug => bug.BugId)
                    .ToList();
            }
        }

        public SourceOfBugsWithoutSpecificReviewer(int repositoryId)
            : base(repositoryId)
        {
            FilteredAuthor = "hg@mozilla.com";
        }
    }
}
