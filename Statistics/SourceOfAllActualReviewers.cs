using ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Statistics
{
    class SourceOfAllActualReviewers : SourceOfActualReviewers
    {
        public override IEnumerable<int> findReviews()
        {
            using (var context = new ExpertiseDBEntities())
                return context.ActualReviewers.Where(ar => ar.RepositoryId == RepositoryId).Select(ar => ar.ActualReviewerId).ToList();
        }

        public override IDictionary<int, string> findReviewsWithReviewers()
        {
            throw new NotImplementedException();
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
