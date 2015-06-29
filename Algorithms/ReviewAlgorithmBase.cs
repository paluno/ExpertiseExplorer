﻿using ExpertiseExplorer.ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms
{
    public abstract class ReviewAlgorithmBase : AlgorithmBase
    {
        /// <summary>
        /// Notify the algorithm about a review, so the algorithm may respect it in its reviewer calculations
        /// </summary>
        /// <param name="dateOfReview">The algorithm uses dateOfReview to judge whether the review was evaluated already and prevent double-evaluations.</param>
        public abstract void AddReviewScore(string authorName, IList<string> involvedFiles, DateTime dateOfReview);

        // This is a strange "solution"
        public override void CalculateExpertiseForFile(string filename)
        {
            // Nothing to do here, it's all done in AddReviewScore
        }

        public override void BuildConnectionsForSourceRepositoryBetween(DateTime start, DateTime end)
        {
            // Nothing to be done here. Review algorithms build the connections on their own.
        }

        public IEnumerable<int> FindOrCreateDeveloperFromDevelopernameApproximation(string developername)
        {
            Debug.Assert(RepositoryId > -1, "Initialize RepositoryId first");

            List<int> foundDeveloperIds = new List<int>();
            using (var repository = new ExpertiseDBEntities())
            {
                foreach (string deanonymizedDeveloperName in Deduplicator.DeanonymizeAuthor(developername))
                {
                    Developer developer = repository.Developers.SingleOrDefault(dev => dev.Name == deanonymizedDeveloperName && dev.RepositoryId == RepositoryId);
                    if (developer == null)
                    {
                        developer = repository.Developers.Add(new Developer()
                        {
                            RepositoryId = RepositoryId,
                            Name = deanonymizedDeveloperName
                        });

                        repository.SaveChanges();
                    }

                    foundDeveloperIds.Add(developer.DeveloperId);
                }
            }

            return foundDeveloperIds;
        }
    }
}
