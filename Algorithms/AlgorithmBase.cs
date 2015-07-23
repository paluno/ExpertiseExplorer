namespace ExpertiseExplorer.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Collections.Concurrent;

    using ExpertiseDB;

    using ExpertiseExplorer.Common;
    using ExpertiseDB.Extensions;
    using ExpertiseExplorer.Algorithms.Statistics;
    using ExpertiseExplorer.Algorithms.RepositoryManagement;

    public abstract class AlgorithmBase
    {
        public string Name { get; protected set; }

        private const int TOTAL_NUMBER_OF_CONCURRENT_TASKS = 30;

        protected static readonly TaskFactory algorithmTaskFactory = new TaskFactory(new LimitedConcurrencyLevelTaskScheduler(TOTAL_NUMBER_OF_CONCURRENT_TASKS));

        public AliasFinder Deduplicator { get; set; }

        public SourceRepositoryConnector SourceRepositoryManager { get; set; }

        public int AlgorithmId { get; protected set; }

        public Guid Guid { get; protected set; }

        /// <summary>
        /// When calculating expertise, the algorithm shall consider only events that happened before MaxDateTime.
        /// This means that the algorithm calculated the expertise that could be known at MaxDateTime.
        /// </summary>
        public DateTime MaxDateTime { get; set; }

        public int RepositoryId { get; set; }

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
                        RepositoryAlgorithmRunStatus rars = entities.RepositoryAlgorithmRunStatus.Find(AlgorithmId, RepositoryId);
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
            MaxDateTime = DateTime.MinValue;
            RepositoryId = -1;

            this.Name = GetType().Name;
            this.Deduplicator = new AliasFinder();
        }

        public virtual void UpdateFromSourceUntil(DateTime end)
        {
            MaxDateTime = end;
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
                Artifact artifact = SourceRepositoryManager.FindOrCreateArtifact(entities, filename, ExpertiseExplorer.Common.ArtifactTypeEnum.File);
                foreach (DeveloperExpertise expertise in artifact.DeveloperExpertises)
                {
                    IEnumerator<DeveloperExpertiseValue> iteratorOnValuesToClear = expertise.DeveloperExpertiseValues.Where(dev => dev.AlgorithmId == AlgorithmId).GetEnumerator();
                    while (iteratorOnValuesToClear.MoveNext())
                        expertise.DeveloperExpertiseValues.Remove(iteratorOnValuesToClear.Current);
                }
                entities.SaveChanges();
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
        }

        /// <summary>
        /// Stores multiple expertise values for one artifact.
        /// </summary>
        /// <param name="filename">The name of the artifact for which the experts are sought</param>
        /// <param name="devIdsWithExpertiseValues">A dictionary that maps DeveloperIds to expertise values</param>
        protected void storeDeveloperExpertiseValues(string filename, IEnumerable<DeveloperWithExpertise> devIdsWithExpertiseValues)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                int artifactId = SourceRepositoryManager.FindOrCreateArtifact(repository, filename, ArtifactTypeEnum.File).ArtifactId;

                bool fNewAdditions = false;

                foreach (DeveloperWithExpertise devExpertise in devIdsWithExpertiseValues)
                {
                    if (fNewAdditions)
                    {
                        repository.SaveChanges();   // The Entity Framework does not seem to like it if multiple new entries are added in the above way.
                        fNewAdditions = false;      // Therefore we save after additions.
                    }

                    DeveloperExpertise developerExpertise = SourceRepositoryManager.FindOrCreateDeveloperExpertise(repository, devExpertise.DeveloperId, artifactId, true);
                    fNewAdditions |= 0 == developerExpertise.DeveloperExpertiseId;  // hack: is it a new DeveloperExpertise?
                    DeveloperExpertiseValue devExpertiseValue = FindOrCreateDeveloperExpertiseValue(developerExpertise);
                    devExpertiseValue.Value = devExpertise.Expertise;
                    fNewAdditions |= 0 == devExpertiseValue.DeveloperExpertiseValueId;  // hack: is it a new DeveloperExpertiseValue?
                }

                repository.SaveChanges();
            }
        }

        protected DeveloperExpertiseValue FindOrCreateDeveloperExpertiseValue(DeveloperExpertise developerExpertise)
        {
            DeveloperExpertiseValue dev = developerExpertise.DeveloperExpertiseValues.SingleOrDefault(
                    devCandidate => devCandidate.AlgorithmId == AlgorithmId);

            if (null == dev)
            {
                dev = new DeveloperExpertiseValue
                {
                    AlgorithmId = AlgorithmId,
                    DeveloperExpertiseId = developerExpertise.DeveloperExpertiseId
                };

                developerExpertise.DeveloperExpertiseValues.Add(dev);
            }

            return dev;
        }

        public virtual Task<ComputedReviewer> GetDevelopersForArtifactsAsync(IEnumerable<int> artifactIds)
        {
            return GetDevelopersForArtifactsAsync(artifactIds, expertises => expertises.Sum());
        }

        public async Task<ComputedReviewer> GetDevelopersForArtifactsAsync(IEnumerable<int> artifactIds, Func<IEnumerable<double>, double> aggregateResults)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                List<SimplifiedDeveloperExpertise> developers = (await entities.GetTop5DevelopersForArtifactsAndAlgorithm(artifactIds, AlgorithmId, aggregateResults)).ToList();

                while (developers.Count < 5)
                    developers.Add(new SimplifiedDeveloperExpertise { DeveloperId = null, Expertise = 0d });

                return new ComputedReviewer
                {
                    Expert1Id = developers[0].DeveloperId,
                    Expert1Value = developers[0].Expertise,
                    Expert2Id = developers[1].DeveloperId,
                    Expert2Value = developers[1].Expertise,
                    Expert3Id = developers[2].DeveloperId,
                    Expert3Value = developers[2].Expertise,
                    Expert4Id = developers[3].DeveloperId,
                    Expert4Value = developers[3].Expertise,
                    Expert5Id = developers[4].DeveloperId,
                    Expert5Value = developers[4].Expertise,
                    AlgorithmId = this.AlgorithmId
                };
            }
        }
    }
}
