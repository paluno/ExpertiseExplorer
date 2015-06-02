using ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms.Statistics
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
        /// <returns>Ids of ComputedReviewers that do not contain any reference to hg@mozilla.com</returns>
        protected override IEnumerable<int> findReviewsInDatabase()
        {
            using (var context = new ExpertiseDBEntities())
            {
                return context.ComputedReviewers
                        // Find all computed reviewers that have no recommendation for hg@mozilla.com
                    .Where(cr => cr.Bug.RepositoryId == RepositoryId && cr.Expert1 != "hg@mozilla.com" && cr.Expert2 != "hg@mozilla.com" && cr.Expert3 != "hg@mozilla.com" && cr.Expert4 != "hg@mozilla.com" && cr.Expert5 != "hg@mozilla.com")
                        // We are interested in ActualReviewerIds, though
                    .Select(cr => cr.ActualReviewerId).Distinct().ToList()
                        // Other ComputedReviewers for the same ActualReview shall also not recommend hg@mozilla.com
                    .Where(id => !context.ComputedReviewers
                        .Where(cr => cr.ActualReviewerId == id)
                        .Any(cr => cr.Expert1 == "hg@mozilla.com" || cr.Expert2 == "hg@mozilla.com" || cr.Expert3 == "hg@mozilla.com" || cr.Expert4 == "hg@mozilla.com" || cr.Expert5 == "hg@mozilla.com"))
                    .ToList();
            }
        }

        public override IDictionary<int, string> findReviewsWithReviewers()
        {
            throw new NotImplementedException();
        }

        public SourceOfActualReviewersWithoutHg(int repositoryId)
            : base(repositoryId)
        {
        }
    }
}
