namespace ExpertiseExplorer.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using System.Data.Entity;

    using ExpertiseDB;
    using ExpertiseExplorer.Common;
    using System.Threading.Tasks;

    public class Line10RuleAlgorithm : AlgorithmBase
    {
        public Line10RuleAlgorithm()
        {
            Guid = new Guid("9d7b1706-3e79-442b-babd-cf7cc405a896");
            Init();
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            Debug.Assert(MaxDateTime != DateTime.MinValue, "Initialize MaxDateTime first");
            Debug.Assert(SourceRepositoryManager != null, "Initialize SourceRepositoryManager first");

            if (!SourceRepositoryManager.FileExists(filename))
            {
                ClearExpertiseForAllDevelopers(filename);   // the file does not exist in the repository, so nobody has experience
                return;
            }

            int filenameId = SourceRepositoryManager.GetFilenameIdFromFilenameApproximation(filename);

            using (var entities = new ExpertiseDBEntities())
            {
                DeveloperWithEditTime lastUser = entities.GetUserForLastRevisionOfBefore(filenameId, MaxDateTime);
                if (lastUser == null)   // the file exists but is has not been edited until MaxDateTime. Thus, nobody has expertise.
                {
                    ClearExpertiseForAllDevelopers(filename);
                    return;
                }

                IEnumerable<DeveloperWithEditTime> listOfLastDevelopers = Deduplicator.DeanonymizeAuthor(lastUser.User)
                    .Select(clearName => new DeveloperWithEditTime() { User = clearName, Time = lastUser.Time });   // probably just one, but maybe more
                
                foreach (DeveloperWithEditTime oneOfTheLastDevelopers in listOfLastDevelopers)
                {
                    int developerId = entities.Developers.Single(d => d.Name == oneOfTheLastDevelopers.User && d.RepositoryId == RepositoryId).DeveloperId;
                    DeveloperExpertise developerExpertise = SourceRepositoryManager.FindDeveloperExpertiseWithArtifactName(entities, developerId, filename);
                    var expertiseValue = FindOrCreateDeveloperExpertiseValue(developerExpertise);
                    expertiseValue.Value = oneOfTheLastDevelopers.Time.UTCDateTime2unixTime();
                }
                entities.SaveChanges();
            }
        }

        public override async Task<ComputedReviewer> GetDevelopersForArtifactsAsync(IEnumerable<int> artifactIds)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                DeveloperExpertiseValue deValue = await entities.DeveloperExpertiseValues
                    .Where(dev => dev.AlgorithmId == AlgorithmId && artifactIds.Contains(dev.DeveloperExpertise.ArtifactId))
                    .OrderByDescending(dev => dev.Value)
                    .FirstOrDefaultAsync();

                int? developerId;
                double developerExpertise;

                if (null == deValue)
                {
                    developerId = null;
                    developerExpertise = 0d;
                }
                else
                {
                    developerId = deValue.DeveloperExpertise.Developer.DeveloperId;
                    developerExpertise = deValue.Value;
                }

                return new ComputedReviewer()
                {
                    Expert1Id = developerId,
                    Expert1Value = developerExpertise,
                    Expert2Value = 0d,
                    Expert3Value = 0d,
                    Expert4Value = 0d,
                    Expert5Value = 0d,
                    AlgorithmId = this.AlgorithmId
                };
            }
        }
    }
}
