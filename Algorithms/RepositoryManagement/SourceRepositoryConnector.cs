using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

using ExpertiseExplorer.Common;
using ExpertiseExplorer.ExpertiseDB;

namespace ExpertiseExplorer.Algorithms.RepositoryManagement
{
    public class SourceRepositoryConnector
    {
        public int SourceRepositoryId { get; set; }

        public int RepositoryId { get; set; }
        
        public AliasFinder Deduplicator { get; set; }

        /// <summary>
        /// This is the DateTime until which the data from the SourceRepository was transferred to the Repository
        /// </summary>
        public DateTime Watermark { get; private set; }

        public SourceRepositoryConnector()
        {
            RepositoryId = -1;
            SourceRepositoryId = -1;
            Watermark = DateTime.MinValue;

            this.Deduplicator = new AliasFinder();
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
                Watermark = repository.LastUpdate ?? DateTime.MinValue;
                SourceRepositoryId = sourceRepository.SourceRepositoryId;
            }
        }
        
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

        #region Updating the Repository from SourceRepository
        /// <summary>
        /// Updates the Repository with data from the SourceRepository. This includes adding or updating DeveloperExpertise entries with the
        /// correct number of Deliveries and Acceptances. The method is idempotent and the watermark can only rise and never fall.
        /// </summary>
        /// <param name="end">Until which time (exclusive) shall the VCS data be transferred?</param>
        public virtual void BuildConnectionsForSourceRepositoryUntil(DateTime end)
        {
            Debug.Assert(RepositoryId != -1, "Set RepositoryId first!");

            if (end < Watermark)
                throw new InvalidOperationException("Watermark cannot fall; Repository has already been updated until " + Watermark + ", but an update is requested until " + end);
            if (end == Watermark)
                return;         // we are already done.

            // rise Watermark to end, so find all revisions in the meantime that have to be transferred from SourceRepository to Repository

            List<Revision> revisions;
            using (var entities = new ExpertiseDBEntities())
            {
                DateTime? lastUpdate = entities.Repositorys.Single(r => r.RepositoryId == RepositoryId).LastUpdate;
                if (lastUpdate.HasValue)
                {
                    Debug.Assert(Watermark >= lastUpdate.Value);    // Watermark is exclusive though, and lastUpdate.Value is inclusive

                    revisions = entities.Revisions.Where(r => r.SourceRepositoryId == SourceRepositoryId && r.Time > lastUpdate.Value && r.Time < end).ToList();
                }
                else
                    revisions = entities.Revisions.Where(r => r.SourceRepositoryId == SourceRepositoryId && r.Time < end).ToList();     // First transfer
            }

            if (revisions.Count == 0)
            {
                Watermark = end;
                return;
            }

            BuildConnectionsFromRevisions(revisions);
            Debug.Assert(Watermark < end);  // because Watermark rises with DB Updates, but they area all before end
            Watermark = end;
        }

        /// <returns>The time of the last revision</returns>
        public void BuildConnectionsFromRevisions(List<Revision> revisions)
        {
            DateTime lastestUpdate = DateTime.MinValue;

            foreach (var revision in revisions.OrderBy(r => r.Time))
            {
                HandleRevision(revision);
                if (revision.Time > lastestUpdate)      // only prevents updates if two succeeding revisions have the same time
                {
                    SetLastUpdated(revision.Time);
                    lastestUpdate = revision.Time;
                }
            }
        }

        private void SetLastUpdated(DateTime updateTime)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                var repo = repository.Repositorys.Find(RepositoryId);
                repo.LastUpdate = updateTime;
                repository.SaveChanges();
            }

            if (Watermark < updateTime)
                Watermark = updateTime;
        }

        private void HandleRevision(Revision revision)
        {
            List<int> developerIds;
            IEnumerable<FileRevision> fileRevisions;
            using (var repository = new ExpertiseDBEntities())
            {
                developerIds = Deduplicator.DeanonymizeAuthor(revision.User)
                    .Select(developerName => repository.GetDeveloperIdFromNameForRepository(developerName, RepositoryId))
                    .ToList();
                fileRevisions = repository.FileRevisions
                    .Include(f => f.Filename)
                    .Where(f => f.RevisionId == revision.RevisionId)
                    .ToList();
            }

            foreach (int developerId in developerIds)
                foreach (FileRevision file in fileRevisions)
                    LinkDeveloperAndArtifact(developerId, file, ArtifactTypeEnum.File);
        }

        private void LinkDeveloperAndArtifact(int developerId, FileRevision fileRevision, ArtifactTypeEnum artifactType)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                string FileName = fileRevision.Filename.Name;

                var developerExpertise = FindOrCreateDeveloperExpertise(repository, developerId, FileName, artifactType, false);

                developerExpertise.Artifact.ModificationCount++;

                if (fileRevision.IsNew)
                    developerExpertise.IsFirstAuthor = true;
                else
                    developerExpertise.DeliveriesCount++;

                repository.SaveChanges();
            }
        }
        #endregion Updating the Repository from SourceRepository

        #region DeveloperExpertises and Artifacts
        private static NameLockFactory artifactLocks = new NameLockFactory();
        private static NameLockFactory developerExpertiseLocksOnArtifacts = new NameLockFactory();

        public DeveloperExpertise FindDeveloperExpertiseWithArtifactName(ExpertiseDBEntities repository, int developerId, string filename)
        {
            return repository.Artifacts.Single(a => a.Name == filename && a.RepositoryId == RepositoryId)  // now we have the artifact
                        .DeveloperExpertises.Single(de => de.DeveloperId == developerId);
        }

        public DeveloperExpertise FindOrCreateDeveloperExpertise(ExpertiseDBEntities repository, int developerId, string filename, ArtifactTypeEnum artifactType, bool isInferred)
        {
            int artifactId = FindOrCreateArtifact(repository, filename, artifactType).ArtifactId;

            return FindOrCreateDeveloperExpertise(repository, developerId, artifactId, isInferred);
        }

        public DeveloperExpertise FindOrCreateDeveloperExpertise(ExpertiseDBEntities repository, int developerId, int artifactId, bool isInferred)
        {
            DeveloperExpertise developerExpertise = repository.DeveloperExpertises.SingleOrDefault(
                de => de.DeveloperId == developerId && de.ArtifactId == artifactId);

            if (developerExpertise == null)
            {
                lock (developerExpertiseLocksOnArtifacts.acquireLock(developerId + "-" + artifactId))
                {
                    using (ExpertiseDBEntities freshRepository = new ExpertiseDBEntities())
                    {
                        developerExpertise = freshRepository.DeveloperExpertises.SingleOrDefault(
                            de => de.DeveloperId == developerId && de.ArtifactId == artifactId);

                        if (null == developerExpertise)
                        {
                            developerExpertise = freshRepository.DeveloperExpertises.Add(
                                new DeveloperExpertise
                                {
                                    ArtifactId = artifactId,
                                    DeveloperId = developerId,
                                    Inferred = isInferred
                                });
                            freshRepository.SaveChanges();
                        }
                    }
                }
                developerExpertiseLocksOnArtifacts.releaseLock(developerId + "-" + artifactId);

                developerExpertise = repository.DeveloperExpertises
                    .Single(de => de.DeveloperId == developerId && de.ArtifactId == artifactId);    // re-retrieve from original repository
            }

            return developerExpertise;
        }

        public int FindOrCreateFileArtifactId(string artifactname)
        {
            Debug.Assert(RepositoryId > -1, "Initialize RepositoryId first");

            using (ExpertiseDBEntities repository = new ExpertiseDBEntities())
                return FindOrCreateArtifact(repository, artifactname, ArtifactTypeEnum.File).ArtifactId;
        }

        public Artifact FindOrCreateArtifact(ExpertiseDBEntities repository, string FileName, ArtifactTypeEnum artifactType)
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
        #endregion DeveloperExpertises and Artifacts
    }
}
