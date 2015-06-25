namespace Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using ExpertiseDB;

    using ExpertiseExplorerCommon;
    using ExpertiseDB.Extensions;
    using System.Collections.Concurrent;
using Algorithms.Statistics;

    public abstract class AlgorithmBase
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string Name { get; protected set; }

        private const int TOTAL_NUMBER_OF_CONCURRENT_TASKS = 30;

        protected static readonly TaskFactory algorithmTaskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(TOTAL_NUMBER_OF_CONCURRENT_TASKS));

        public AliasFinder Deduplicator { get; set; }

        public int AlgorithmId { get; protected set; }

        public Guid Guid { get; protected set; }

        /// <summary>
        /// When calculating expertise, the algorithm shall consider only events that happened before MaxDateTime.
        /// This means that the algorithm calculated the expertise that could be known at MaxDateTime.
        /// </summary>
        public DateTime MaxDateTime { get; set; }

        public int RepositoryId { get; set; }

        public int SourceRepositoryId { get; set; }

        private DateTime? _RunUntil;
        /// <summary>
        /// The algorithm's "watermark" to indicate until which date this algorithm was computed. The algorithms usees this for critical operations
        /// to make themselves idempotent. Automatically persists to database.
        /// </summary>
        protected DateTime RunUntil
        {
            get
            {
                if (null == _RunUntil)
                    using (ExpertiseDBEntities entities = new ExpertiseDBEntities())
                    {
                        RepositoryAlgorithmRunStatus rars = entities.RepositoryAlgorithmRunStatus.Find(RepositoryId, AlgorithmId);
                        if (null == rars)
                            _RunUntil = DateTime.MinValue;
                        else
                            _RunUntil = rars.RunUntil;
                    }

                return (DateTime)_RunUntil;
            }
            set
            {
                if (RunUntil > value)
                    throw new ArgumentOutOfRangeException("RunUntil", "RunUntil watermark can only rise, but new value " + value + " is smaller than current value " + RunUntil);

                _RunUntil = value;

                using (ExpertiseDBEntities entities = new ExpertiseDBEntities())
                {
                    RepositoryAlgorithmRunStatus rars = entities.RepositoryAlgorithmRunStatus.Find(AlgorithmId, RepositoryId);
                    if (null == rars)
                    {
                        rars = new RepositoryAlgorithmRunStatus() { AlgorithmId = this.AlgorithmId, RepositoryId = this.RepositoryId, RunUntil = value };
                        entities.RepositoryAlgorithmRunStatus.Add(rars);
                    }
                    else if (rars.RunUntil < value)
                        rars.RunUntil = value;
                    else
                        return;

                    entities.SaveChanges();
                }
            }
        }

        protected AlgorithmBase()
        {
            this.Name = GetType().Name;
            this.Deduplicator = new AliasFinder();
        }
        
        public abstract void CalculateExpertiseForFile(string filename);

        public async Task CalculateExpertiseForFilesAsync(IEnumerable<string> filenames)
        {
            var tasks = new List<Task>();

            foreach (var filename in filenames)
            {
                if (filename == string.Empty)
                    continue;

                tasks.Add(
                    algorithmTaskFactory.StartNew(
                        input => CalculateExpertiseForFile(input as string),
                        filename,
                            TaskCreationOptions.AttachedToParent));
            }

            await Task.WhenAll(tasks);
        }

        protected void ClearExpertiseForAllDevelopers(string filename)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                Artifact artifact = FindOrCreateArtifact(entities, filename, ExpertiseExplorerCommon.ArtifactTypeEnum.File);
                foreach (DeveloperExpertise expertise in artifact.DeveloperExpertises)
                {
                    IEnumerator<DeveloperExpertiseValue> iteratorOnValuesToClear = expertise.DeveloperExpertiseValues.Where(dev => dev.AlgorithmId == AlgorithmId).GetEnumerator();
                    while(iteratorOnValuesToClear.MoveNext())
                        expertise.DeveloperExpertiseValues.Remove(iteratorOnValuesToClear.Current);
                }
                entities.SaveChanges();
            }
        }

        public void InitIdsFromDbForSourceUrl(string sourceUrl, bool failIfAlreadyExists)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                var sourceRepository = entities.SourceRepositorys.SingleOrDefault(sr => sr.URL == sourceUrl);
                if (sourceRepository == null)
                    throw new FileNotFoundException("Source repository not found.");

                var repository = entities.Repositorys.SingleOrDefault(r => r.SourceURL == sourceUrl);
                if (repository == null)
                {
                    repository =
                        entities.Repositorys.Add(
                            new Repository
                            {
                                Name = sourceRepository.Name,
                                SourceURL = sourceUrl
                            });

                    entities.SaveChanges();
                }
                else
                    if (failIfAlreadyExists) throw new Exception("Already exists!");

                RepositoryId = repository.RepositoryId;
                SourceRepositoryId = sourceRepository.SourceRepositoryId;
            }
        }

        protected void Init()
        {
            using (var repository = new ExpertiseDBEntities())
            {
                var algorithm = repository.Algorithms.SingleOrDefault(a => a.GUID == Guid);
                if (algorithm == null)
                {
                    algorithm = repository.Algorithms.Add(new Algorithm { Name = this.Name, GUID = Guid });
                    repository.SaveChanges();
                }

                AlgorithmId = algorithm.AlgorithmId;
            }

            MaxDateTime = DateTime.MinValue;
            RepositoryId = -1;
            SourceRepositoryId = -1;
        }

#region Updating Repository from SourceRepository
        public virtual void BuildConnectionsForSourceRepositoryBetween(DateTime start, DateTime end)
        {
            Debug.Assert(RepositoryId != -1, "Set RepositoryId first!");

            List<Revision> revisions;
            using (var entities = new ExpertiseDBEntities())
            {
                var lastUpdate = entities.Repositorys.Single(r => r.RepositoryId == RepositoryId).LastUpdate;
                if (lastUpdate.HasValue)
                {
                    if (lastUpdate.Value >= end)
                    {
                        MaxDateTime = lastUpdate.Value;
                        return;
                    }

                    if (start < lastUpdate.Value) start = lastUpdate.Value;

                    revisions = entities.GetRevisionsFromSourceRepositoryBetween(SourceRepositoryId, start, end);
                    if (revisions.Count == 0)
                    {
                        MaxDateTime = lastUpdate.Value;
                        return;
                    }
                }
                else
                    revisions = entities.GetRevisionsFromSourceRepositoryBetween(SourceRepositoryId, start, end);
                if (revisions.Count == 0)
                {
                    MaxDateTime = end;
                    return;
                }
            }

            BuildConnectionsFromRevisions(revisions);
        }

        public void BuildConnectionsFromRevisions(List<Revision> revisions)
        {
            var orderedRevisions = revisions.OrderBy(r => r.Time).ToList();
            MaxDateTime = orderedRevisions.Last().Time;

            foreach (var revision in orderedRevisions)
            {
                HandleRevision(revision);
                SetLastUpdated(revision.Time);
            }
        }

        private List<Revision> GetRevisionsFromSourceRepository()
        {
            List<Revision> revisions;
            using (var repository = new ExpertiseDBEntities())
            {
                revisions = repository.Revisions.Where(r => r.SourceRepositoryId == SourceRepositoryId).ToList();
            }

            return revisions;
        }

        private void SetLastUpdated(DateTime updateTime)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                var repo = repository.Repositorys.Find(RepositoryId);
                repo.LastUpdate = updateTime;
                repository.SaveChanges();
            }
        }

        private void HandleRevision(Revision revision)
        {
            List<int> developerIds;
            List<int> fileRevisionIds;
            using (var repository = new ExpertiseDBEntities())
            {
                developerIds = Deduplicator.DeanonymizeAuthor(revision.User)
                    .Select(developerName => repository.GetDeveloperIdFromNameForRepository(developerName, RepositoryId))
                    .ToList();
                fileRevisionIds = repository.FileRevisions.Where(f => f.RevisionId == revision.RevisionId).Select(fi => fi.FileRevisionId).ToList();
            }

            foreach (var developerId in developerIds)
            {
                foreach (var fileId in fileRevisionIds)
                {
                    HandleFileRevisions(fileId, developerId);
                }
            }
        }

        private void HandleFileRevisions(int fileRevisionId, int developerId)
        {
            FileRevision file;
            using (var repository = new ExpertiseDBEntities())
            {
                file = repository.FileRevisions.Include(f => f.Filename).Single(f => f.FileRevisionId == fileRevisionId);
            }

            LinkDeveloperAndArtifact(developerId, file, ArtifactTypeEnum.File);
        }

        private void LinkDeveloperAndArtifact(int developerId, FileRevision fileRevision, ArtifactTypeEnum artifactType)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                string FileName = fileRevision.Filename.Name;

                var developerExpertise = FindOrCreateDeveloperExpertise(repository, developerId, FileName, artifactType);

                developerExpertise.Artifact.ModificationCount++;

                if (fileRevision.IsNew)
                    developerExpertise.IsFirstAuthor = true;
                else
                    developerExpertise.DeliveriesCount++;

                repository.SaveChanges();
            }
        }
#endregion Updating Repository from SourceRepository

        public int GetFilenameIdFromFilenameApproximation(string filename)
        {
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");

            using (var repository = new ExpertiseDBEntities())
            {
                var file = repository.Filenames.SingleOrDefault(f => f.Name == filename && f.SourceRepositoryId == SourceRepositoryId);
                if (file == null)
                    throw new ArgumentException("The file \"" + filename + "\" does not exist in the repository.", "filename");

                return file.FilenameId;
            }
        }

        public int FindOrCreateFileArtifactId(string artifactname)
        {
            Debug.Assert(RepositoryId > -1, "Initialize RepositoryId first");

            using (ExpertiseDBEntities repository = new ExpertiseDBEntities())
                return FindOrCreateArtifact(repository, artifactname, ArtifactTypeEnum.File).ArtifactId;
        }


        /// <summary>
        /// Stores multiple expertise values for one artifact.
        /// </summary>
        /// <param name="filename">The name of the artifact for which the experts are sought</param>
        /// <param name="devIdsWithExpertiseValues">A dictionary that maps DeveloperIds to expertise values</param>
        protected void storeDeveloperExpertiseValues(string filename, IEnumerable<KeyValuePair<int, Double>> devIdsWithExpertiseValues)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                //int artifactId = FindOrCreateArtifact(repository,filename, ArtifactTypeEnum.File).ArtifactId;

                bool fNewAdditions = false;

                foreach (KeyValuePair<int, Double> pair in devIdsWithExpertiseValues)
                {
                    if (fNewAdditions)
                    {
                        repository.SaveChanges();   // The Entity Framework does not seem to like it if multiple new entries are added in the above way.
                        fNewAdditions = false;      // Therefore we save after additions.
                    }

                    DeveloperExpertise developerExpertise = FindOrCreateDeveloperExpertise(repository, pair.Key, filename, ArtifactTypeEnum.File);
                    fNewAdditions |= 0 == developerExpertise.DeveloperExpertiseId;  // hack: is it a new DeveloperExpertise?
                    DeveloperExpertiseValue devExpertiseValue = FindOrCreateDeveloperExpertiseValue(repository, developerExpertise);
                    devExpertiseValue.Value = pair.Value;
                    fNewAdditions |= 0 == devExpertiseValue.DeveloperExpertiseValueId;  // hack: is it a new DeveloperExpertiseValue?
                }

                repository.SaveChanges();
            }
        }

        private static NameLockFactory artifactLocks = new NameLockFactory();

        protected DeveloperExpertise FindOrCreateDeveloperExpertise(ExpertiseDBEntities repository, int developerId, string FileName, ArtifactTypeEnum artifactType)
        {
            var developerExpertise = repository.DeveloperExpertises.SingleOrDefault(
                de => de.DeveloperId == developerId && de.Artifact.Name == FileName);

            if (developerExpertise == null)
            {
                Artifact artifact = FindOrCreateArtifact(repository, FileName, artifactType);

                // There is exactly one thread for each developer/artifact combination if this method was called from FPSAlgorithm. Therefore only one thread
                // tries to create this specific DeveloperExpertise entry. Thus, no thread safety measures must be taken.
                // If this method is called from LinkDeveloperAndArtifact or WeighedReviewCountAlgorithm, there is only one thread.

                developerExpertise = repository.DeveloperExpertises.Add(
                    new DeveloperExpertise
                    {
                        Artifact = artifact,
                        DeveloperId = developerId
                    });
            }

            return developerExpertise;
        }

        protected Artifact FindOrCreateArtifact(ExpertiseDBEntities repository, string FileName, ArtifactTypeEnum artifactType)
        {
            Artifact artifact = repository.Artifacts.SingleOrDefault(a => a.Name == FileName && a.RepositoryId == RepositoryId);

            if (null == artifact)   // thread-safe artifact insertion
            {
                lock (artifactLocks.acquireLock(FileName))
                {
                    using (ExpertiseDBEntities freshRepository = new ExpertiseDBEntities())
                    {
                        artifact = freshRepository.Artifacts.SingleOrDefault(a => a.Name == FileName && a.RepositoryId == RepositoryId);
                        if (null == artifact)
                        {
                            artifact = new Artifact { ArtifactTypeId = (int)artifactType, Name = FileName, RepositoryId = RepositoryId };
                            freshRepository.Artifacts.Add(artifact);
                            freshRepository.SaveChanges();
                        }
                    }
                }
                artifactLocks.releaseLock(FileName);

                artifact = repository.Artifacts.SingleOrDefault(a => a.Name == FileName && a.RepositoryId == RepositoryId); // re-retrieve from other repository
            }
            return artifact;
        }

        protected DeveloperExpertiseValue FindOrCreateDeveloperExpertiseValue(ExpertiseDBEntities repository, DeveloperExpertise developerExpertise)
        {
            return
                developerExpertise.DeveloperExpertiseValues.SingleOrDefault(
                    dev => dev.AlgorithmId == AlgorithmId) ?? repository.DeveloperExpertiseValues.Add(
                        new DeveloperExpertiseValue
                        {
                            AlgorithmId = AlgorithmId,
                            DeveloperExpertiseId = developerExpertise.DeveloperExpertiseId
                        });
        }

        public virtual async Task<ComputedReviewer> GetDevelopersForArtifactsAsync(IEnumerable<int> artifactIds)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                List<SimplifiedDeveloperExpertise> developers = await entities.GetDevelopersForArtifactsAndAlgorithm(artifactIds, AlgorithmId)
                    .OrderByDescending(sde => sde.Expertise)
                    .Take(5)
                    .ToListAsync();

                while (developers.Count < 5)
                    developers.Add(new SimplifiedDeveloperExpertise { DeveloperId = 0, DeveloperName = string.Empty, Expertise = 0d });

                return new ComputedReviewer
                {
                    Expert1 = developers[0].DeveloperName,
                    Expert1Value = developers[0].Expertise,
                    Expert2 = developers[1].DeveloperName,
                    Expert2Value = developers[1].Expertise,
                    Expert3 = developers[2].DeveloperName,
                    Expert3Value = developers[2].Expertise,
                    Expert4 = developers[3].DeveloperName,
                    Expert4Value = developers[3].Expertise,
                    Expert5 = developers[4].DeveloperName,
                    Expert5Value = developers[4].Expertise,
                    AlgorithmId = this.AlgorithmId
                };
            }
        }
    }
}
