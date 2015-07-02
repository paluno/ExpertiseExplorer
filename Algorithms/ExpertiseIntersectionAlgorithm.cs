namespace ExpertiseExplorer.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using ExpertiseDB;
    using ExpertiseExplorer.Common;
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

            using (var repository = new ExpertiseDBEntities())
            {
                IEnumerable<DeveloperWithEditTime> authors = repository.GetUsersOfRevisionsOfBefore(filenameId, MaxDateTime);

                if (!authors.Any())
                {
                    ClearExpertiseForAllDevelopers(filename);
                    return;
                }

                    // cleanup author list
                authors = authors
                    .SelectMany(oneOfTheLastUsers => Deduplicator.DeanonymizeAuthor(oneOfTheLastUsers.User)
                        .Select(clearName => new DeveloperWithEditTime() { User = clearName, Time = oneOfTheLastUsers.Time }))
                    .OrderByDescending(dev => dev.Time);
                ISet<string> includedAuthors = new HashSet<string>();
                IList<DeveloperWithEditTime> deduplicatedAuthors = new List<DeveloperWithEditTime>();
                foreach(DeveloperWithEditTime dev in authors)
                    if (!includedAuthors.Contains(dev.User))
                        deduplicatedAuthors.Add(dev);

                foreach (DeveloperWithEditTime experiencedDeveloper in deduplicatedAuthors)
                {
                    var developerId = repository.Developers.Single(d => d.Name == experiencedDeveloper.User && d.RepositoryId == RepositoryId).DeveloperId;
                    var developerExpertise = repository.DeveloperExpertises.Include(de => de.DeveloperExpertiseValues).Single(de => de.DeveloperId == developerId && de.ArtifactId == artifactId);
                    var expertiseValue = FindOrCreateDeveloperExpertiseValue(developerExpertise);
                    expertiseValue.Value = experiencedDeveloper.Time.UTCDateTime2unixTime();
                }

                repository.SaveChanges();
            }
        }

        public override async Task<ComputedReviewer> GetDevelopersForArtifactsAsync(IEnumerable<int> artifactIds)
        {
            List<SimplifiedDeveloperExpertise> deValues;
            using (var entities = new ExpertiseDBEntities())
            {
                deValues = await entities.DeveloperExpertiseValues
                    .Include(de => de.DeveloperExpertise)
                    .Where(dev => artifactIds.Contains(dev.DeveloperExpertise.ArtifactId) && dev.AlgorithmId == AlgorithmId)
                    .AsNoTracking()
                    .GroupBy(
                        dev => dev.DeveloperExpertise.DeveloperId,
                        (devId, expertiseValues) => new SimplifiedDeveloperExpertise()
                        {
                            DeveloperId = devId,
                            Expertise = expertiseValues.Select(exValue => exValue.Value).Max()
                        })
                    .OrderByDescending(sde => sde.Expertise)
                    .Take(5)
                    .ToListAsync();
            }

            while (deValues.Count < 5)
                deValues.Add(new SimplifiedDeveloperExpertise { DeveloperId = null, Expertise = 0d });

            return new ComputedReviewer()
            {
                Expert1Id = deValues[0].DeveloperId,
                Expert1Value = deValues[0].Expertise,
                Expert2Id = deValues[1].DeveloperId,
                Expert2Value = deValues[1].Expertise,
                Expert3Id = deValues[2].DeveloperId,
                Expert3Value = deValues[2].Expertise,
                Expert4Id = deValues[3].DeveloperId,
                Expert4Value = deValues[3].Expertise,
                Expert5Id = deValues[4].DeveloperId,
                Expert5Value = deValues[4].Expertise,
                AlgorithmId = this.AlgorithmId
            };
        }

    }
}
