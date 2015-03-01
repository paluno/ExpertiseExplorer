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

    public abstract class AlgorithmBase
    {
        protected static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const int NumberOfTasks = 5;

        private TaskFactory taskFactory;

        public int AlgorithmId { get; protected set; }

        public Guid Guid { get; protected set; }

        public DateTime MaxDateTime { get; set; }

        public int RepositoryId { get; set; }

        public int SourceRepositoryId { get; set; }

        public abstract void CalculateExpertise();

        public abstract void CalculateExpertiseForFile(string filename);

        public void CalculateExpertiseForFiles(List<string> filenames)
        {
            var tasks = new List<Task>();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var filename in filenames)
            {
                if (filename == string.Empty)
                    continue;

                tasks.Add(
                    taskFactory.StartNew(
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

        public void BuildConnectionsForSourceRepositoryBetween(string sourceUrl, DateTime start, DateTime end)
        {
            InitIdsFromDbForSourceUrl(sourceUrl, false);
            BuildConnectionsForSourceRepositoryBetween(start, end);
        }

        public void BuildConnectionsForSourceRepositoryBetween(DateTime start, DateTime end)
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

            taskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(NumberOfTasks));
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
            var artifactTypeId = (int)artifactType;

            using (var repository = new ExpertiseDBEntities())
            {
                var developerExpertise = repository.DeveloperExpertises.SingleOrDefault(
                    de => de.DeveloperId == developerId && de.Artifact.Name == fileRevision.Filename.Name);

                if (developerExpertise == null)
                {
                    var artifact = repository.Artifacts.SingleOrDefault(a => a.Name == fileRevision.Filename.Name && a.RepositoryId == RepositoryId)
                                   ??
                                   new Artifact { ArtifactTypeId = artifactTypeId, Name = fileRevision.Filename.Name, RepositoryId = RepositoryId };

                    developerExpertise = repository.DeveloperExpertises.Add(
                        new DeveloperExpertise
                        {
                            Artifact = artifact,
                            DeveloperId = developerId
                        });
                }

                developerExpertise.Artifact.ModificationCount++;

                if (fileRevision.IsNew)
                    developerExpertise.IsFirstAuthor = true;
                else 
                    developerExpertise.DeliveriesCount++;

                repository.SaveChanges();
            }
        }
    }
}
