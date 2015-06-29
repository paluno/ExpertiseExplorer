using ExpertiseExplorer.ExpertiseDB;
using ExpertiseExplorer.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms
{
    /// <summary>
    /// Calculates the weighed number of reviews for the files. The weight comes in if multiple files are reviewed at once, in which case
    /// each only receives its share of the count. This algorithm is the foundation for other algorithms like FPSReview.
    /// </summary>
    public class WeighedReviewCountAlgorithm : ReviewAlgorithmBase
    {
        internal static Guid WEIGHEDREVIEWCOUNTGUID = new Guid("9F03D6DB-7DE8-4826-9B69-7E79C766959D");

        private FPS.RootDirectory FpsTree { get; set; }

        public WeighedReviewCountAlgorithm(FPS.RootDirectory fpsTree)
        {
            this.FpsTree = fpsTree;
            Guid = WEIGHEDREVIEWCOUNTGUID;
            Init();
        }

        public void LoadReviewScoresFromDB()
        {
            using (var repository = new ExpertiseDBEntities())
            {
                foreach (DeveloperExpertiseValue dev in repository.DeveloperExpertiseValues
                    .Include("DeveloperExpertise.Artifact")
                    .Include("DeveloperExpertise.Developer")
                    .Where(dev => dev.AlgorithmId == AlgorithmId && dev.DeveloperExpertise.Artifact.RepositoryId == RepositoryId))
                    FpsTree.AddReview(dev.DeveloperExpertise.Developer.Name, dev.DeveloperExpertise.Artifact.Name.Split('/'), dev.Value);
            }
        }

        public override void AddReviewScore(string authorName, IList<string> involvedFiles, DateTime dateOfReview)
        {
            if (dateOfReview < RunUntil)
                return;     // prevent double evaluation of reviews

            int numberOfFiles = involvedFiles.Count;

            int idReviewer = FindOrCreateDeveloperFromDevelopernameApproximation(authorName);

                // write to tree
            FpsTree.AddReview(authorName, involvedFiles);

                // write to DB
            foreach (string reviewedFileName in involvedFiles)
            {
                using (var repository = new ExpertiseDBEntities())
                {
                    DeveloperExpertise devExpertise = FindOrCreateDeveloperExpertise(repository, idReviewer, reviewedFileName, ArtifactTypeEnum.File);

                    DeveloperExpertiseValue currentWeightedReviewValue = FindOrCreateDeveloperExpertiseValue(repository, devExpertise);
                    if (double.IsNaN(currentWeightedReviewValue.Value))
                        currentWeightedReviewValue.Value = (1D / numberOfFiles);
                    else
                        currentWeightedReviewValue.Value += (1D / numberOfFiles);

                    repository.SaveChanges();
                }
            }

            RunUntil = dateOfReview;
        }
    }
}
