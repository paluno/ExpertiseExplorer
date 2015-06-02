using ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms.Statistics
{
    class SourceOfAllActualReviewers : SourceOfActualReviewers
    {
        protected override IEnumerable<int> findReviewsInDatabase()
        {
            using (var context = new ExpertiseDBEntities())
                return context.ActualReviewers.Where(ar => ar.Bug.RepositoryId == RepositoryId).Select(ar => ar.ActualReviewerId).ToList();
        }

        public override IDictionary<int, string> findReviewsWithReviewers()
        {
            using (var context = new ExpertiseDBEntities())
                return context.ActualReviewers.Where(ar => ar.Bug.RepositoryId == RepositoryId).ToDictionary(ar => ar.ActualReviewerId, ar => ar.Reviewer);
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
