namespace Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using System.Data.Entity;

    using ExpertiseDB;
    using ExpertiseExplorerCommon;
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
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");

            int filenameId;
            try
            {
                filenameId = GetFilenameIdFromFilenameApproximation(filename);
            }
            catch (ArgumentException ae)
            {
                if (ae.ParamName != "filename")
                    throw;
                ClearExpertiseForAllDevelopers(filename);   // the file does not exist in the repository, so nobody has experience
                return;
            }

            //int artifactId = FindOrCreateFileArtifactId(filename);
            //if (artifactId < 0)
            //    throw new FileNotFoundException(string.Format("Artifact {0} not found", filename));

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

                IEnumerable<int> lastDeveloperIds = entities.Developers.Where(d => d.Name == lastUser.User && d.RepositoryId == RepositoryId).Select(d => d.DeveloperId);

                foreach (int oneOfTheLastDevelopers in lastDeveloperIds)
                {
                    DeveloperExpertise developerExpertise = FindOrCreateDeveloperExpertise(entities, oneOfTheLastDevelopers, filename, ExpertiseExplorerCommon.ArtifactTypeEnum.File);
                    var expertiseValue = FindOrCreateDeveloperExpertiseValue(entities, developerExpertise);
                    expertiseValue.Value = lastUser.Time.PDTDateTime2unixTime();
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

                string developerName;
                double developerExpertise;

                if (null == deValue)
                {
                    developerName = string.Empty;
                    developerExpertise = 0d;
                }
                else
                {
                    developerName = deValue.DeveloperExpertise.Developer.Name;
                    developerExpertise = deValue.Value;
                }

                return new ComputedReviewer()
                {
                    Expert1 = developerName,
                    Expert1Value = developerExpertise,
                    Expert2 = string.Empty,
                    Expert2Value = 0d,
                    Expert3 = string.Empty,
                    Expert3Value = 0d,
                    Expert4 = string.Empty,
                    Expert4Value = 0d,
                    Expert5 = string.Empty,
                    Expert5Value = 0d,
                    AlgorithmId = this.AlgorithmId
                };
            }
        }
    }
}
