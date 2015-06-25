namespace Algorithms
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

        public override void CalculateExpertiseForFile(string filename)
        {
            Debug.Assert(MaxDateTime != DateTime.MinValue, "Initialize MaxDateTime first");
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");

            int artifactId = FindOrCreateFileArtifactId(filename);

            var path = Path.GetDirectoryName(filename);
            if (path == null)
                throw new NullReferenceException("path");

            List<DeveloperForPath> developersForPath;
            if (path == string.Empty)
            {
                using (var repository = new ExpertiseDBEntities())
                {
                    developersForPath = repository.GetDeveloperWithoutPath(RepositoryId);
                }
            }
            else
            {
                path = path.Replace("\\", "/");
                path = path + "/";

                using (var repository = new ExpertiseDBEntities())
                {
                    developersForPath = repository.GetDeveloperForPath(RepositoryId,path);
                }
            }

            IEnumerable<DeveloperWithExpertise> experiencedDevelopers = developersForPath
                .Select(dev4path => new DeveloperWithExpertise(dev4path.DeveloperId,dev4path.DeliveriesCount + dev4path.IsFirstAuthorCount));
            storeDeveloperExpertiseValues(filename, experiencedDevelopers);
        }
    }
}
