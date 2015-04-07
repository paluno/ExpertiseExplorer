namespace Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using ExpertiseDB;

    public class Line10RuleAlgorithm : AlgorithmBase
    {
        public Line10RuleAlgorithm()
        {
            Guid = new Guid("9d7b1706-3e79-442b-babd-cf7cc405a896");
            Init();
        }

        public override void CalculateExpertise()
        {
            Debug.Assert(MaxDateTime != DateTime.MinValue, "Initialize MaxDateTime first");
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");

            base.CalculateExpertise();
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            Debug.Assert(MaxDateTime != DateTime.MinValue, "Initialize MaxDateTime first");
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");
            var stopwatch = new Stopwatch();

            var filenameId = GetFilenameIdFromFilenameApproximation(filename);
            if (filenameId < 0)
                throw new FileNotFoundException(string.Format("Filename {0} not found", filename));

            var artifactId = GetArtifactIdFromArtifactnameApproximation(filename);
            if (artifactId < 0)
                throw new FileNotFoundException(string.Format("Artifact {0} not found", filename));

            string lastUser;
            stopwatch.Start();
            using (var entities = new ExpertiseDBEntities())
            {
                lastUser = entities.GetUserForLastRevisionOfBefore(filenameId, MaxDateTime);
            }

            stopwatch.Stop();
            Log.Info(GetType() + " - GetUserForLastRevisionOfBefore() - " + stopwatch.Elapsed);

            if (lastUser == null)
                throw new FileNotFoundException(string.Format("LastRevision for {0} not found", filename));

            stopwatch.Start();
            using (var entities = new ExpertiseDBEntities())
            {
                var lastDeveloperId = entities.Developers.Where(d => d.Name == lastUser && d.RepositoryId == RepositoryId).Select(d => d.DeveloperId).First();

                var developerIds = entities.DeveloperExpertises.Where(de => de.ArtifactId == artifactId && de.Inferred == false).Select(de => de.DeveloperId).Distinct().ToList();
                foreach (var developerId in developerIds)
                {
                    var fixMyClosure = developerId;
                    var developerExpertise = entities.DeveloperExpertises.Where(de => de.DeveloperId == fixMyClosure && de.ArtifactId == artifactId).First();

                    var expertiseValue = FindOrCreateDeveloperExpertiseValue(entities, developerExpertise);

                    // reset all connected developer's expertise to 0
                    expertiseValue.Value = 0f;
                    
                    // except of the last one's who did a modification to the artifact
                    if (developerId == lastDeveloperId)
                        expertiseValue.Value = 1f;
                }
                
                entities.SaveChanges();
            }

            stopwatch.Stop();
            Log.Info(GetType() + " - SaveChanges() - " + stopwatch.Elapsed);
        }
    }
}
