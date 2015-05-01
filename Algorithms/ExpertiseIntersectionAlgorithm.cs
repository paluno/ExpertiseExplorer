﻿namespace Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using ExpertiseDB;

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
            var stopwatch = new Stopwatch();

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

            var orderedAuthorIds = new List<int>();
            stopwatch.Start();
            using (var repository = new ExpertiseDBEntities())
            {
                var authors = repository.GetUsersOfRevisionsOfBefore(filenameId, MaxDateTime);

                if (authors.Count == 0)
                {
                    ClearExpertiseForAllDevelopers(filename);
                    return;
                }

                var alreadyIn = new HashSet<string>();
                foreach (var author in authors)
                {
                    if (alreadyIn.Contains(author)) continue;

                    var developerId = repository.Developers.Single(d => d.Name == author && d.RepositoryId == RepositoryId).DeveloperId;
                    if (!orderedAuthorIds.Contains(developerId))
                        orderedAuthorIds.Add(developerId);

                    alreadyIn.Add(author);
                }
            }

            stopwatch.Stop();
            Log.Info(Name + " - GetUsersOfRevisionsOfBefore() - " + stopwatch.Elapsed);

            // HACK: to implement the ordering via only the Expertise Value
            var authorRankLookup = new Dictionary<int, int>();
            for (var i = 0; i < orderedAuthorIds.Count; i++)
            {
                authorRankLookup.Add(orderedAuthorIds[i], i);
            }

            stopwatch.Start();
            using (var repository = new ExpertiseDBEntities())
            {
                //var developers = repository.DeveloperExpertises.Where(de => de.ArtifactId == artifactId && de.Inferred == false).Select(de => de.DeveloperId).Distinct().ToList();
                foreach (var developerId in orderedAuthorIds)
                {
                    var developerExpertise = repository.DeveloperExpertises.Include(de => de.DeveloperExpertiseValues).Single(de => de.DeveloperId == developerId && de.ArtifactId == artifactId);

                    var expertiseValue = FindOrCreateDeveloperExpertiseValue(repository, developerExpertise);

                    expertiseValue.Value = developerExpertise.DeliveriesCount + (developerExpertise.IsFirstAuthor ? 1f : 0f) + (authorRankLookup[developerId] * 100000f);
                }

                repository.SaveChanges();
            }

            stopwatch.Stop();
            Log.Info(Name + " - SaveChanges() - " + stopwatch.Elapsed);
        }
    }
}
