namespace Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using ExpertiseDB;
    using ExpertiseExplorerCommon;
    using ExpertiseDB.Extensions;

    /// <summary>
    /// Implements the algorithm described by McDonald and Ackerman in "Expertise Recommender".
    /// Their recommendation list contains the number of changes to the module and the date of last change.
    /// However, it is orderer only by date of last change, so we can ignore that their implementation
    /// contains additional information (the number of changes) intended for the human user.
    /// 
    /// Additionally, they specify that an expert must have edited all modules that are queried for.
    /// If modules were seen as files, then this would meand that an expert must have changed all
    /// files. However, the definition is open for interpretation and we choose a module to be larger,
    /// containing all files. Thus, experts may have only touched any number of relevant files.
    /// </summary>
    public class ExpertiseIntersectionAlgorithm : AlgorithmBase
    {
        public ExpertiseIntersectionAlgorithm()
        {
            Guid = new Guid("05c6aa8a-dcd3-405b-9759-d51a7fbade4d");
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

            int artifactId = FindOrCreateFileArtifactId(filename);

            //var orderedAuthorIds = new List<int>();
            List<DeveloperWithEditTime> authors;
            using (var repository = new ExpertiseDBEntities())
            {
                authors = repository.GetUsersOfRevisionsOfBefore(filenameId, MaxDateTime);

                if (authors.Count == 0)
                {
                    ClearExpertiseForAllDevelopers(filename);
                    return;
                }

                foreach (DeveloperWithEditTime experiencedDeveloper in authors)
                {
                    var developerId = repository.Developers.Single(d => d.Name == experiencedDeveloper.User && d.RepositoryId == RepositoryId).DeveloperId;
                    var developerExpertise = repository.DeveloperExpertises.Include(de => de.DeveloperExpertiseValues).Single(de => de.DeveloperId == developerId && de.ArtifactId == artifactId);
                    var expertiseValue = FindOrCreateDeveloperExpertiseValue(repository, developerExpertise);
                    expertiseValue.Value = experiencedDeveloper.Time.PDTDateTime2unixTime();
                }

                repository.SaveChanges();
            }
        }

        public override ComputedReviewer GetDevelopersForArtifacts(IEnumerable<int> artifactIds)
        {
            List<SimplifiedDeveloperExpertise> deValues;
            using (var entities = new ExpertiseDBEntities())
            {
                deValues = entities.DeveloperExpertiseValues
                    .Include(dev => dev.DeveloperExpertise.Developer).Include(de => de.DeveloperExpertise)
                    .Where(dev => artifactIds.Contains(dev.DeveloperExpertise.ArtifactId) && dev.AlgorithmId == AlgorithmId)
                    .AsNoTracking()
                    .GroupBy(
                        dev => dev.DeveloperExpertise.DeveloperId,
                        (devId, expertiseValues) => new SimplifiedDeveloperExpertise()
                        {
                            DeveloperId = devId,
                            DeveloperName = expertiseValues.First().DeveloperExpertise.Developer.Name,
                            Expertise = expertiseValues.Select(exValue => exValue.Value).Max()
                        })
                    .OrderByDescending(sde => sde.Expertise)
                    .Take(5)
                    .ToList();
            }

            while (deValues.Count < 5)
                deValues.Add(new SimplifiedDeveloperExpertise { DeveloperId = 0, DeveloperName = string.Empty, Expertise = 0d });

            return new ComputedReviewer()
            {
                Expert1 = deValues[0].DeveloperName,
                Expert1Value = deValues[0].Expertise,
                Expert2 = deValues[1].DeveloperName,
                Expert2Value = deValues[1].Expertise,
                Expert3 = deValues[2].DeveloperName,
                Expert3Value = deValues[2].Expertise,
                Expert4 = deValues[3].DeveloperName,
                Expert4Value = deValues[3].Expertise,
                Expert5 = deValues[4].DeveloperName,
                Expert5Value = deValues[4].Expertise,
                AlgorithmId = this.AlgorithmId
            };
        }

    }
}
