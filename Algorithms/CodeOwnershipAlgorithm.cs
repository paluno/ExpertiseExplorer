namespace Algorithms
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

        public override void CalculateExpertise()
        {
            Debug.Assert(MaxDateTime != DateTime.MinValue, "Initialize MaxDateTime first");
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");
            Debug.Assert(RepositoryId > -1, "Initialize RepositoryId first");

            List<string> filenames;
            using (var repository = new ExpertiseDBEntities())
            {
                filenames = repository.Artifacts.Where(a => a.RepositoryId == RepositoryId).Select(f => f.Name).ToList();
            }

            CalculateExpertiseForFiles(filenames);
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            Debug.Assert(MaxDateTime != DateTime.MinValue, "Initialize MaxDateTime first");
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");
            var stopwatch = new Stopwatch();

            var filenameId = GetFilenameIdFromFilenameApproximation(filename);
            if (filenameId < 0)
                throw new FileNotFoundException(string.Format("Filename {0} not found", filename));

            List<FileRevision> fileRevisions;
            stopwatch.Start();
            using (var repository = new ExpertiseDBEntities())
            {
                fileRevisions = repository.FileRevisions.Include(fr => fr.Revision).Where(f => f.FilenameId == filenameId && f.Revision.Time <= MaxDateTime).OrderBy(f => f.Revision.Time).AsNoTracking().ToList();
            }

            stopwatch.Stop();
            Log.Info(GetType() + " - fileRevisions - " + stopwatch.Elapsed);

            if (fileRevisions.Count == 0)
                throw new FileNotFoundException(string.Format("LastRevision for {0} not found", filename));

            // first author is handles seperately
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
            var artifactId = GetArtifactIdFromArtifactnameApproximation(filename);
            if (artifactId < 0)
                throw new FileNotFoundException(string.Format("Artifact {0} not found", filename));

            var developerLookup = new Dictionary<string, DeveloperExpertiseValue>();
            stopwatch.Start();
            using (var repository = new ExpertiseDBEntities())
            {
                var developers = repository.DeveloperExpertises.Where(de => de.ArtifactId == artifactId && de.Inferred == false).Select(de => de.DeveloperId).Distinct().ToList();

                foreach (var developerId in developers)
                {
                    var developer = repository.Developers.Single(d => d.DeveloperId == developerId);
                    var developerExpertise =
                        repository.DeveloperExpertises.Include(de => de.DeveloperExpertiseValues).Single(
                            de => de.DeveloperId == developerId && de.ArtifactId == artifactId);

                    var expertiseValue =
                        developerExpertise.DeveloperExpertiseValues.SingleOrDefault(
                            dev => dev.AlgorithmId == AlgorithmId)
                        ??
                        repository.DeveloperExpertiseValues.Add(
                            new DeveloperExpertiseValue
                                {
                                    AlgorithmId = AlgorithmId,
                                    DeveloperExpertiseId = developerExpertise.DeveloperExpertiseId
                                });

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

            stopwatch.Stop();
            Log.Info(GetType() + " - SaveChanges() - " + stopwatch.Elapsed);
        }
    }
}
