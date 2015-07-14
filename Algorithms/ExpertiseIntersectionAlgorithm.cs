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
                        // deanonymize
                authors = authors
                    .SelectMany(oneOfTheLastUsers => Deduplicator.DeanonymizeAuthor(oneOfTheLastUsers.User)
                        .Select(clearName => new DeveloperWithEditTime() { User = clearName, Time = oneOfTheLastUsers.Time }))
                    .OrderByDescending(dev => dev.Time);
                        // deduplicate deanonymized names
                ISet<string> includedAuthors = new HashSet<string>();
                IList<DeveloperWithEditTime> deduplicatedAuthors = new List<DeveloperWithEditTime>();
                foreach(DeveloperWithEditTime dev in authors)
                    if (includedAuthors.Add(dev.User))
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

        public override Task<ComputedReviewer> GetDevelopersForArtifactsAsync(IEnumerable<int> artifactIds)
        {
            return GetDevelopersForArtifactsAsync(artifactIds, expertises => expertises.Max());
        }

    }
}
