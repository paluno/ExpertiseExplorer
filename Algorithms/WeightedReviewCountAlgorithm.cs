using ExpertiseExplorer.ExpertiseDB;
using ExpertiseExplorer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ExpertiseExplorer.Algorithms
{
    /// <summary>
    /// Calculates the weighted number of reviews for the files. The weight comes in if multiple files are reviewed at once, in which case
    /// each only receives its share of the count. This algorithm is the foundation for other algorithms like FPSReview.
    /// </summary>
    public class WeightedReviewCountAlgorithm : ReviewAlgorithmBase
    {
        internal static Guid WEIGHTEDREVIEWCOUNTGUID = new Guid("9F03D6DB-7DE8-4826-9B69-7E79C766959D");

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Store the weights also in an optimized tree for better performance than DB queries
        /// </summary>
        private FPS.RootDirectory FpsTree { get; set; }

        public WeightedReviewCountAlgorithm(FPS.RootDirectory fpsTree)
        {
            this.FpsTree = fpsTree;
            Guid = WEIGHTEDREVIEWCOUNTGUID;
            Init();
        }

        public void LoadReviewScoresFromDB()
        {
            using (var repository = new ExpertiseDBEntities())
            {
                foreach (DeveloperExpertiseValue dev in repository.DeveloperExpertiseValues
                    .Include("DeveloperExpertise.Artifact")
                    .Where(dev => dev.AlgorithmId == AlgorithmId && dev.DeveloperExpertise.Artifact.RepositoryId == RepositoryId))
                    FpsTree.AddReview(dev.DeveloperExpertise.DeveloperId, dev.DeveloperExpertise.Artifact.Name.Split('/'), dev.Value);
            }
        }

        /// <summary>
        /// Who was the author in the last run? Used to determine whether a review is doubly evaluated or whether there were two differen reviews taking place at the same time
        /// </summary>
        private string lastAuthor;

        public override void AddReviewScore(string authorName, IList<string> involvedFiles, DateTime dateOfReview)
        {
            if (dateOfReview < RunUntil)
                return;     // prevent double evaluation of reviews
            if (dateOfReview <= RunUntil.AddSeconds(1)  // Add one second to prevent errors from time skew
                && (lastAuthor == authorName            // no reviewer can review two patches at the same time.
                    || null == lastAuthor))             // If this is a resume and the first review, we assume that two reviews will not take place at the same time (which is very seldom, if ever, the case)
            {
                log.Warn("Skipping a review weighting that happened at " + dateOfReview.ToUniversalTime().ToString("u") 
                    + " by " + authorName + ", because the last review was at (nearly) the same time, at " + RunUntil.ToUniversalTime().ToString("u")
                    + ", and had the author " + (lastAuthor ?? "(no author, first after resume)"));
                return;
            }
            lastAuthor = authorName;
            
            int numberOfFiles = involvedFiles.Count;

            IEnumerable<int> idReviewers = FindOrCreateDeveloperFromDevelopernameApproximation(authorName); // usually just one

                // write to tree
            foreach (int reviewerId in idReviewers)
            {
                FpsTree.AddReview(reviewerId, involvedFiles);

                // write to DB
                foreach (string reviewedFileName in involvedFiles)
                {
                    using (var repository = new ExpertiseDBEntities())
                    {
                        DeveloperExpertise devExpertise = SourceRepositoryManager.FindOrCreateDeveloperExpertise(repository, reviewerId, reviewedFileName, ArtifactTypeEnum.File, true);

                        DeveloperExpertiseValue currentWeightedReviewValue = FindOrCreateDeveloperExpertiseValue(devExpertise);
                        if (double.IsNaN(currentWeightedReviewValue.Value))
                            currentWeightedReviewValue.Value = (1D / numberOfFiles);
                        else
                            currentWeightedReviewValue.Value += (1D / numberOfFiles);

                        repository.SaveChanges();
                    }
                }
            }

            RunUntil = dateOfReview;
        }
    }
}
