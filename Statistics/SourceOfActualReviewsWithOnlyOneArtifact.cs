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

        public override IEnumerable<int> findReviews()
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
            throw new NotImplementedException();
        }

        public SourceOfActualReviewsWithOnlyOneArtifact(int repositoryId)
            : base (repositoryId)
        { }
    }
}
