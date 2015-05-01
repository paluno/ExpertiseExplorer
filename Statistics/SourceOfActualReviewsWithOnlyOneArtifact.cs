using ExpertiseDB;
using ExpertiseDB.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statistics
{
    class SourceOfActualReviewsWithOnlyOneArtifact : SourceOfActualReviewers
    {
        public override string Postfix
        {
            get { return "_only_one"; }
        }

        protected override IEnumerable<int> findReviewsInDatabase()
        {
            var result = new List<int>();

            using (var context = new ExpertiseDBEntities())
            {
                List<ActualReviewersGrouped> actualReviewersGrouped = context.GetActualReviewersGrouped(RepositoryId)
                                                                        .Where(arg => arg.Count == 1).ToList();

                result.AddRange(from reviewersGrouped in actualReviewersGrouped where reviewersGrouped.Count == 1 select context.ActualReviewers.First(ar => ar.ChangeId == reviewersGrouped.ChangeId).ActualReviewerId);
            }

            return result;
        }

        public override IDictionary<int, string> findReviewsWithReviewers()
        {
            using (var context = new ExpertiseDBEntities())
            {
                return context.GetActualReviewersGrouped(RepositoryId)
                                .Where(arg => arg.Count == 1)
                                .Select(reviewersGrouped => context.ActualReviewers.First(ar => ar.ChangeId == reviewersGrouped.ChangeId))
                                .ToDictionary(ar => ar.ActualReviewerId, ar => ar.Reviewer);
            }
        }

        public SourceOfActualReviewsWithOnlyOneArtifact(int repositoryId)
            : base (repositoryId)
        { }
    }
}
