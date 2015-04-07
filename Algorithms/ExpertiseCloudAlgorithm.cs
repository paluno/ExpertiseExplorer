﻿namespace Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using ExpertiseDB;

    public class ExpertiseCloudAlgorithm : AlgorithmBase
    {
        public ExpertiseCloudAlgorithm()
        {
            Guid = new Guid("f2fd950d-3b01-4db0-88a9-8a38f34ae0c4");
            Init();
        }

        public override void CalculateExpertise()
        {
            Debug.Assert(MaxDateTime != DateTime.MinValue, "Initialize MaxDateTime first");
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");
            Debug.Assert(RepositoryId > -1, "Initialize RepositoryId first");

            base.CalculateExpertise();
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            Debug.Assert(MaxDateTime != DateTime.MinValue, "Initialize MaxDateTime first");
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");

            var artifactId = GetArtifactIdFromArtifactnameApproximation(filename);
            if (artifactId < 0)
                throw new FileNotFoundException(string.Format("Artifact {0} not found", filename));

            var path = Path.GetDirectoryName(filename);
            if (path == null)
                throw new NullReferenceException("path");

            List<DeveloperForPath> developersForPath;
            if (path == string.Empty)
            {
                using (var repository = new ExpertiseDBEntities())
                {
                    developersForPath = repository.GetDeveloperWithoutPath();
                }
            }
            else
            {
                path = path.Replace("\\", "/");
                path = path + "/";

                using (var repository = new ExpertiseDBEntities())
                {
                    developersForPath = repository.GetDeveloperForPath(path);
                }
            }

            using (var repository = new ExpertiseDBEntities())
            {
                foreach (var developerForPath in developersForPath)
                {
                    var developerExpertise = repository.DeveloperExpertises.Include(de => de.DeveloperExpertiseValues).SingleOrDefault(de => de.DeveloperId == developerForPath.DeveloperId && de.ArtifactId == artifactId);
                    if (developerExpertise == null)
                    {
                        developerExpertise = repository.DeveloperExpertises.Add(
                            new DeveloperExpertise
                            {
                                ArtifactId = artifactId,
                                DeveloperId = developerForPath.DeveloperId,
                                Inferred = true
                            });

                        repository.DeveloperExpertises.Add(developerExpertise);
                        repository.SaveChanges();

                        developerExpertise = repository.DeveloperExpertises.Include(de => de.DeveloperExpertiseValues).Single(de => de.DeveloperId == developerForPath.DeveloperId && de.ArtifactId == artifactId);
                    }

                    var expertiseValue = FindOrCreateDeveloperExpertiseValue(repository, developerExpertise);

                    expertiseValue.Value = developerForPath.DeliveriesCount + developerForPath.IsFirstAuthorCount;
                }

                repository.SaveChanges();
            }
        }
    }
}
