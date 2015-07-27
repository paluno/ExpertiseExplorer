namespace ExpertiseExplorer.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using ExpertiseDB;
    using ExpertiseDB.Extensions;

    public class ExpertiseCloudAlgorithm : AlgorithmBase
    {
        public ExpertiseCloudAlgorithm()
        {
            Guid = new Guid("f2fd950d-3b01-4db0-88a9-8a38f34ae0c4");
            Init();
        }
        public override void UpdateFromSourceUntil(DateTime end)
        {
            SourceRepositoryManager.BuildConnectionsForSourceRepositoryUntil(end);
            base.UpdateFromSourceUntil(end);
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            Debug.Assert(MaxDateTime != DateTime.MinValue, "Initialize MaxDateTime first");
            Debug.Assert(SourceRepositoryManager != null, "Initialize SourceRepositoryManager first");

            int artifactId = SourceRepositoryManager.FindOrCreateFileArtifactId(filename);

            string path;
            try
            {
                path = Path.GetDirectoryName(filename);
            }
            catch(System.ArgumentException) when (Path.GetInvalidPathChars().Any(evilChar => filename.Contains(evilChar)))
            {
                string escapedFilename = filename;
                foreach (char evilChar in Path.GetInvalidPathChars())
                    escapedFilename = escapedFilename.Replace(evilChar, '%');
                path = Path.GetDirectoryName(escapedFilename);
            }
            if (path == null)
                throw new NullReferenceException("path from file " + filename + " is null");

            IEnumerable<DeveloperForPath> developersForPath;
            if (path == string.Empty)
            {
                using (var repository = new ExpertiseDBEntities())
                {
                    developersForPath = repository.GetDevelopersWithoutPath(RepositoryId);
                }
            }
            else
            {
                path = path.Replace("\\", "/");
                path = path + "/";

                using (var repository = new ExpertiseDBEntities())
                {
                    developersForPath = repository.GetDevelopersForPath(RepositoryId, path);
                }
            }

            IEnumerable<DeveloperWithExpertise> experiencedDevelopers = developersForPath
                .Select(dev4path => new DeveloperWithExpertise(dev4path.DeveloperId, dev4path.DeliveriesCount + dev4path.IsFirstAuthorCount));
            storeDeveloperExpertiseValues(filename, experiencedDevelopers);
        }
    }
}
