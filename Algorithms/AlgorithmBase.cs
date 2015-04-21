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

    public abstract class AlgorithmBase
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const int NumberOfTasks = 5;

        protected TaskFactory TaskFactory { get; private set; }

        public int AlgorithmId { get; protected set; }

        public Guid Guid { get; protected set; }

        public DateTime MaxDateTime { get; set; }

        public int RepositoryId { get; set; }

        public int SourceRepositoryId { get; set; }

        public abstract void CalculateExpertiseForFile(string filename);

        public virtual void CalculateExpertise()
        {
            List<string> filenames;
            using (var repository = new ExpertiseDBEntities())
            {
                filenames = repository.Artifacts.Select(a => a.Name).ToList();
            }

            CalculateExpertiseForFiles(filenames);
        }

        public void CalculateExpertiseForFiles(IEnumerable<string> filenames)
        {
            var tasks = new List<Task>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var filename in filenames)
            {
                if (filename == string.Empty)
                    continue;

                tasks.Add(
                    TaskFactory.StartNew(
                        input => CalculateExpertiseForFile(input as string),
                        filename,
                            TaskCreationOptions.AttachedToParent));
            }

            Task.WaitAll(tasks.ToArray());
            stopwatch.Stop();
            Log.Info(GetType() + " - " + stopwatch.Elapsed);
        }

        public void BuildConnectionsFromSourceUrl(string sourceUrl, bool failIfAlreadyExists = true)
        {
            InitIdsFromDbForSourceUrl(sourceUrl, failIfAlreadyExists);
            BuildConnectionsFromRevisions(GetRevisionsFromSourceRepository());
        }

        //public void BuildConnectionsForSourceRepositoryBetween(string sourceUrl, DateTime start, DateTime end)
        //{
        //    InitIdsFromDbForSourceUrl(sourceUrl, false);
        //    BuildConnectionsForSourceRepositoryBetween(start, end);
        //}

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

        public int GetFilenameIdFromFilenameApproximation(string filename)
        {
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");

            using (var repository = new ExpertiseDBEntities())
            {
                var file = repository.Filenames.SingleOrDefault(f => f.Name == filename && f.SourceRepositoryId == SourceRepositoryId);
                if (file == null)
                {
                    var files = repository.Filenames.Where(f => f.Name.EndsWith(filename) && f.SourceRepositoryId == SourceRepositoryId).ToList();
                    switch (files.Count)
                    {
                        case 0:
                            return -1;
                            
                        case 1:
                            return files.First().FilenameId;
                            
                        default:
                            return -2;
                    }
                }

                return file.FilenameId;
            }
        }

        public int GetArtifactIdFromArtifactnameApproximation(string artifactname)
        {
            Debug.Assert(RepositoryId > -1, "Initialize RepositoryId first");

            using (var repository = new ExpertiseDBEntities())
            {
                var artifact = repository.Artifacts.SingleOrDefault(a => a.Name == artifactname && a.RepositoryId == RepositoryId);
                if (artifact == null)
                {
                    var artifacts = repository.Artifacts.Where(a => a.Name.EndsWith(artifactname) && a.RepositoryId == RepositoryId).ToList();
                    switch (artifacts.Count)
                    {
                        case 0:
                            return -1;

                        case 1:
                            return artifacts.First().ArtifactId;

                        default:
                            return -2;
                    }
                }

                return artifact.ArtifactId;
            }
        }

        public void InitIdsFromDbForSourceUrl(string sourceUrl, bool failIfAlreadyExists)
        {
            using (var entites = new ExpertiseDBEntities())
            {
                var sourceRepository = entites.SourceRepositorys.SingleOrDefault(sr => sr.URL == sourceUrl);
                if (sourceRepository == null)
                    throw new FileNotFoundException("Source repository not found.");

                var repository = entites.Repositorys.SingleOrDefault(r => r.SourceURL == sourceUrl);
                if (repository == null)
                {
                    repository =
                        entites.Repositorys.Add(
                            new Repository
                            {
                                Name = sourceRepository.Name,
                                SourceURL = sourceUrl
                            });

                    entites.SaveChanges();
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
                    algorithm = repository.Algorithms.Add(new Algorithm { Name = GetType().Name, GUID = Guid });
                    repository.SaveChanges();
                }

                AlgorithmId = algorithm.AlgorithmId;
            }

            MaxDateTime = DateTime.MinValue;
            RepositoryId = -1;
            SourceRepositoryId = -1;

            TaskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(NumberOfTasks));
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
                developerIds = repository.GetDeveloperIdFromNameForRepository(revision.User, RepositoryId);
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

        /// <summary>
        /// Stores locks for each artifact by artifact name. A counter exists for every artifact and the dicitionary empties itself again if 
        /// the locks are not needed anymore. Access only through acquireArtifactLock and releaseArtifactLock!
        /// </summary>
        private static readonly ConcurrentDictionary<string, Tuple<int, object>> dictArtifactLocks = new ConcurrentDictionary<string, Tuple<int, object>>();
        private static readonly ConcurrentDictionary<string, int> dictArtifactLockCounter = new ConcurrentDictionary<string, int>();

        private static object acquireArtifactLock(string artifactName)
        {
            Tuple<int, object> lockWithCounter = dictArtifactLocks.AddOrUpdate(
                artifactName, 
                new Tuple<int,object>(1, new object()),     // this is a new lock pair
                delegate(string theName, Tuple<int,object> lockPair) {
                    if (lockPair.Item1 > 0)
                        return new Tuple<int, object>(lockPair.Item1 + 1, lockPair.Item2);  // there are a number of reference already, everything's okay, just increase the counter
                    else
                        return lockPair;    // the lock was supposed to be deleted. Proceed with caution!
                }
            );

            if (0 == lockWithCounter.Item1)    // deletion was in progress
                lock (dictArtifactLocks)
                {
                    lockWithCounter = dictArtifactLocks.AddOrUpdate(
                        artifactName,
                        new Tuple<int, object>(1, new object()),     // this is a new lock pair, the old one was deleted already
                        (theName, lockPair) => new Tuple<int, object>(lockPair.Item1 + 1, lockPair.Item2)   // it's okay even to increase a zero counter because "release" will check before deletion if at all
                    );
                }

            return lockWithCounter.Item2;
        }

        private static void releaseArtifactLock(string artifactName)
        {
            Tuple<int, object> remainingLock = dictArtifactLocks.AddOrUpdate(
                artifactName,
                delegate(string theName) { throw new InvalidOperationException("A lock was released more often than retrieved"); },
                (theName, lockPair) => new Tuple<int, object>(lockPair.Item1 - 1, lockPair.Item2)   // decrease counter
            );

            if (0 == remainingLock.Item1)    // nobody uses the lock anymore, we can delete it
                lock(dictArtifactLocks)
                {
                    if (dictArtifactLocks[artifactName].Item1 > 0)
                        return;     // the lock was recreated in between and must not be released

                    Tuple<int, object> dummy;
                    dictArtifactLocks.TryRemove(artifactName, out dummy);   // this always succeeds
                }
        }

        protected DeveloperExpertise FindOrCreateDeveloperExpertise(ExpertiseDBEntities repository, int developerId, string FileName, ArtifactTypeEnum artifactType)
        {
            var artifactTypeId = (int)artifactType;

            var developerExpertise = repository.DeveloperExpertises.SingleOrDefault(
                de => de.DeveloperId == developerId && de.Artifact.Name == FileName);
            
            if (developerExpertise == null)
            {
                Artifact artifact = repository.Artifacts.SingleOrDefault(a => a.Name == FileName && a.RepositoryId == RepositoryId);
                if (null == artifact)   // thread-safe artifact insertion
                {
                    lock(acquireArtifactLock(FileName))
                    {
                        using (ExpertiseDBEntities freshRepository = new ExpertiseDBEntities())
                        {
                            artifact = freshRepository.Artifacts.SingleOrDefault(a => a.Name == FileName && a.RepositoryId == RepositoryId);
                            if (null == artifact)
                                artifact = new Artifact { ArtifactTypeId = artifactTypeId, Name = FileName, RepositoryId = RepositoryId };
                            freshRepository.Artifacts.Add(artifact);
                            freshRepository.SaveChanges();
                        }
                    }
                    releaseArtifactLock(FileName);

                    artifact = repository.Artifacts.SingleOrDefault(a => a.Name == FileName && a.RepositoryId == RepositoryId); // re-retrieve from other repository
                }

                developerExpertise = repository.DeveloperExpertises.Add(
                    new DeveloperExpertise
                    {
                        Artifact = artifact,
                        DeveloperId = developerId
                    });
            }

            return developerExpertise;
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

        public virtual ComputedReviewer GetDevelopersForArtifact(int artifactId)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                var developers = entities.GetDevelopersForArtifactAndAlgorithm(artifactId, AlgorithmId).OrderByDescending(sde => sde.Expertise).Take(5).ToList();
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
