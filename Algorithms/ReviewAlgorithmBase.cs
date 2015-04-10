using ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms
{
    public abstract class ReviewAlgorithmBase : AlgorithmBase
    {
        /// <summary>
        /// Notify the algorithm about a review, so the algorithm may respect it in its reviewer calculations
        /// </summary>
        public abstract void AddReviewScore(string authorName, IList<string> involvedFiles);

        // This is a strange "solution"
        public override void CalculateExpertiseForFile(string filename)
        {
            // Nothing to do here, it's all done in AddReviewScore
        }

        public override void BuildConnectionsForSourceRepositoryBetween(DateTime start, DateTime end)
        {
            // Nothing to be done here. Review algorithms build the connections on their own.
        }

        public int FindOrCreateDeveloperFromDevelopernameApproximation(string developername)
        {
            Debug.Assert(RepositoryId > -1, "Initialize RepositoryId first");

            using (var repository = new ExpertiseDBEntities())
            {
                Developer developer = repository.Developers.SingleOrDefault(dev => dev.Name == developername && dev.RepositoryId == RepositoryId);
                if (developer == null)
                {
                    // TODO: Some smarter searching. Like using lists with matching developer names or checking whether the part in front of the domain matches

                    developer = repository.Developers.Add(new Developer()
                    {
                        RepositoryId = RepositoryId,
                        Name = developername
                    });

                    repository.SaveChanges();
                }

                return developer.DeveloperId;
            }
        }
    }
}
