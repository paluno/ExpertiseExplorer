﻿namespace Algorithms
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

            int artifactId;
            int lastDeveloperId;
            int filenameId = GetFilenameIdFromFilenameApproximation(filename);
            if (filenameId < 0)
            {
                ClearExpertiseForAllDevelopers(filename);
                return;
            }

            artifactId = FindOrCreateFileArtifactIdFromArtifactnameApproximation(filename);
            if (artifactId < 0)
                throw new FileNotFoundException(string.Format("Artifact {0} not found", filename));

            string lastUser;
            using (var entities = new ExpertiseDBEntities())
            {
                lastUser = entities.GetUserForLastRevisionOfBefore(filenameId, MaxDateTime);

                if (lastUser == null)
                    throw new FileNotFoundException(string.Format("LastRevision for {0} not found", filename));

                lastDeveloperId = entities.Developers.Where(d => d.Name == lastUser && d.RepositoryId == RepositoryId).Select(d => d.DeveloperId).First();
            }


            using (var entities = new ExpertiseDBEntities())
            {

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
        }
    }
}
