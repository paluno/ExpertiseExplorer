namespace ExpertiseExplorer.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using ExpertiseDB;

    public class CodeOwnershipAlgorithm : AlgorithmBase
    {
        public CodeOwnershipAlgorithm()
        {
            Guid = new Guid("8710e841-539d-4481-abda-3ba1b6c37f2b");
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

            int filenameId;
            try
            {
                filenameId = SourceRepositoryManager.GetFilenameIdFromFilenameApproximation(filename);
            }
            catch(ArgumentException ae)
            {
                if (ae.ParamName != "filename")
                    throw;
                ClearExpertiseForAllDevelopers(filename);   // the file does not exist in the repository, so nobody has experience
                return;
            }

            List<FileRevision> fileRevisions;
            using (var repository = new ExpertiseDBEntities())
            {
                fileRevisions = repository.FileRevisions.Include(fr => fr.Revision).Where(f => f.FilenameId == filenameId && f.Revision.Time < MaxDateTime).OrderBy(f => f.Revision.Time).AsNoTracking().ToList();
            }

            if (fileRevisions.Count == 0)
            {
                // no file revisions yet => no prior changes => nobody knows anything about the file
                ClearExpertiseForAllDevelopers(filename);
                return;
            }

            // first author is handled seperately
            var added = fileRevisions[0].LinesAdded;
            fileRevisions.RemoveAt(0);

            // first pass to compute file size frome revision data
            int minsize = int.MaxValue, computedsize = 0;
            foreach (var file in fileRevisions)
            {
                computedsize = computedsize + added - file.LinesDeleted;
                minsize = Math.Min(computedsize, minsize);

                added = file.LinesAdded;
            }

            computedsize = Math.Abs(minsize);

            // second pass to compute the actual ownership
            int artifactId = SourceRepositoryManager.FindOrCreateFileArtifactId(filename);

            var developerLookup = new Dictionary<string, DeveloperExpertiseValue>();
            using (var repository = new ExpertiseDBEntities())
            {
                List<Developer> developers = repository.DeveloperExpertises
                    .Where(de => de.ArtifactId == artifactId && de.Inferred == false && (de.DeliveriesCount > 0 || de.IsFirstAuthor))
                    .Select(de => de.Developer).Distinct().ToList();

                foreach (var developer in developers)
                {
                    var developerExpertise =
                        repository.DeveloperExpertises.Include(de => de.DeveloperExpertiseValues).Single(
                            de => de.DeveloperId == developer.DeveloperId && de.ArtifactId == artifactId);

                    var expertiseValue = FindOrCreateDeveloperExpertiseValue(developerExpertise);

                    expertiseValue.Value = developerExpertise.IsFirstAuthor ? 1f : 0f;

                    // inside loop to get updated generated Ids
                    repository.SaveChanges();

                    developerLookup.Add(developer.Name, expertiseValue);
                }

                foreach (var file in fileRevisions)
                {
                    computedsize = computedsize + file.LinesAdded - file.LinesDeleted;
                    foreach (var kvp in developerLookup)
                    {
                        var expertiseValue = kvp.Value;
                        expertiseValue.Value = expertiseValue.Value * (computedsize - file.LinesDeleted) / computedsize;
                        expertiseValue.Value = double.IsInfinity(expertiseValue.Value) ? 0 : expertiseValue.Value;
                        expertiseValue.Value = double.IsNaN(expertiseValue.Value) ? 0 : expertiseValue.Value;

                        if (kvp.Key == file.Revision.User)
                            expertiseValue.Value = expertiseValue.Value + (file.LinesAdded / (double)computedsize);

                        expertiseValue.Value = double.IsNaN(expertiseValue.Value) ? 0 : expertiseValue.Value;
                        expertiseValue.Value = double.IsInfinity(expertiseValue.Value) ? 0 : expertiseValue.Value;
                    }
                }

                repository.SaveChanges();
            }
        }
    }
}
