namespace ExpertiseDB
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;

    using ExpertiseDB.Extensions;

    public partial class ExpertiseDBEntities
    {
        public ExpertiseDBEntities(string connectionString)
            : base(connectionString)
        {
            ((IObjectContextAdapter)this).ObjectContext.CommandTimeout = 240;
        }

        public List<int> GetDeveloperIdFromNameForRepository(string name, int repositoryId)
        {
            // TODO: place this in a custom filter
            name = name.Replace("plus ", string.Empty);
            name = name.Replace("and the rest of the Xiph.Org Foundation", string.Empty);
            name = name.Replace(" and ", ",");
            
            var names = name.Contains(',') ? name.Split(',') : new[] { name };

            var result = new List<int>();
            foreach (var n in names)
            {
                var devName = n.Trim();
                var developer = Developers.SingleOrDefault(d => d.Name == devName && d.RepositoryId == repositoryId);
                if (developer == null)
                {
                    developer = Developers.Add(new Developer { Name = devName, RepositoryId = repositoryId });
                    SaveChanges();
                }

                result.Add(developer.DeveloperId);
            }

            return result;
        }

        public List<Tuple<string, int, double>> GetTopDeveopersForRepository(int repositoryId, int numberOfHits = 0)
        {
            var developers = GetDeveloperExpertiseSumForRepository(repositoryId, numberOfHits);

            return (from developerExpertiseSum in developers let developerName = Developers.Single(d => d.DeveloperId == developerExpertiseSum.DeveloperId).Name select new Tuple<string, int, double>(developerName, developerExpertiseSum.DeveloperId, developerExpertiseSum.ExSum)).ToList();
        }

        public List<Tuple<string, int, double>> GetDevelopersForArtifact(int artifactId)
        {
            var developerExpertises = DeveloperExpertises.Include(de => de.Developer).Include(de => de.DeveloperExpertiseValues).Where(de => de.ArtifactId == artifactId).AsNoTracking();
            var result = new List<Tuple<string, int, double>>();
            foreach (var developerExpertise in developerExpertises)
            {
                result.AddRange(developerExpertise.DeveloperExpertiseValues.Select(expertise => new Tuple<string, int, double>(developerExpertise.Developer.Name, developerExpertise.DeveloperId, expertise.Value)));
            }

            return result;
        }

        public List<SimplifiedDeveloperExpertise> GetDevelopersForArtifactAndAlgorithm(int artifactId, int algorithmId)
        {
            var developerExpertises = DeveloperExpertises.Include(de => de.Developer).Include(de => de.DeveloperExpertiseValues).Where(de => de.ArtifactId == artifactId).AsNoTracking();
            var result = new List<SimplifiedDeveloperExpertise>();
            foreach (var developerExpertise in developerExpertises)
            {
                result.AddRange(from expertise in developerExpertise.DeveloperExpertiseValues where expertise.AlgorithmId == algorithmId select new SimplifiedDeveloperExpertise { DeveloperName = developerExpertise.Developer.Name, DeveloperId = developerExpertise.DeveloperId, Expertise = expertise.Value });
            }

            return result;
        }

        public List<Tuple<string, int, double>> GetArtifactsForDeveloper(int developerId)
        {
            var developerExpertises = DeveloperExpertises.Include(de => de.Artifact).Include(de => de.DeveloperExpertiseValues).Where(de => de.DeveloperId == developerId).AsNoTracking();
            var result = new List<Tuple<string, int, double>>();
            foreach (var developerExpertise in developerExpertises)
            {
                result.AddRange(developerExpertise.DeveloperExpertiseValues.Select(expertise => new Tuple<string, int, double>(developerExpertise.Artifact.Name, developerExpertise.ArtifactId, expertise.Value)));
            }

            return result;
        }

        public List<Tuple<string, int, double>> GetSoleExpertArtifactsForDeveloper(int developerId)
        {
            var developerExpertises = DeveloperExpertises.Include(de => de.Artifact).Include(de => de.DeveloperExpertiseValues).Where(de => de.DeveloperId == developerId && de.Artifact.DeveloperExpertises.Count < 2).AsNoTracking();
            var result = new List<Tuple<string, int, double>>();
            foreach (var developerExpertise in developerExpertises)
            {
                result.AddRange(developerExpertise.DeveloperExpertiseValues.Select(expertise => new Tuple<string, int, double>(developerExpertise.Artifact.Name, developerExpertise.ArtifactId, expertise.Value)));
            }

            return result;
        }

        public List<Tuple<string, int>> GetArtifactsWithJustOneExpertForRepository(int repositoryId)
        {
            var artifacts = Artifacts.Include(a => a.DeveloperExpertises).Where(a => a.RepositoryId == repositoryId && a.DeveloperExpertises.Count < 2).AsNoTracking();

                // LINQ to Entities does not support constructors with parameters, therefore the intermediate anonymous type
                // exists to show LINQ to Entities which attributes are required from the server
            return artifacts
                .Select(artifact => new { artifact.Name, artifact.ArtifactId }).AsEnumerable()
                .Select(artifact => new Tuple<string, int>(artifact.Name, artifact.ArtifactId)).ToList();
        }

        public List<Tuple<string, int>> GetFilesForRepositoryThatStartWithChar(int repositoryId, string startWith)
        {
            var query = Artifacts.Where(a => a.RepositoryId == repositoryId && a.Name.StartsWith(startWith));

                // LINQ to Entities does not support constructors with parameters, therefore the intermediate anonymous type
                // exists to show LINQ to Entities which attributes are required from the server
            return query
                .Select(artifact => new { artifact.Name, artifact.ArtifactId }).AsEnumerable()
                .Select(artifact => new Tuple<string, int>(artifact.Name, artifact.ArtifactId)).ToList();
        }

        public List<Revision> GetRevisionsFromSourceRepositoryBetween(int sourceRepositoryId, DateTime start, DateTime end)
        {
            return Revisions.Where(r => r.SourceRepositoryId == sourceRepositoryId && r.Time >= start && r.Time < end).ToList();
        }

        public string GetUserForLastRevisionOfBefore(int filenameId, DateTime before)
        {
            var sqlFormattedDate = before.ToString("yyyy-MM-dd HH:mm:ss");
            var sql = string.Format("CALL GetUserForLastRevisionOfBefore({0}, '{1}')", filenameId, sqlFormattedDate);
            var name = Database.SqlQuery<string>(sql).SingleOrDefault();

            // TODO: place this in a custom filter
            name = name.Replace("plus ", string.Empty);
            name = name.Replace("and the rest of the Xiph.Org Foundation", string.Empty);
            name = name.Replace(" and ", ",");

            return name.Contains(',') ? name.Split(',')[0].Trim() : name.Trim();
        }

        public List<string> GetUsersOfRevisionsOfBefore(int filenameId, DateTime before)
        {
            var sqlFormattedDate = before.ToString("yyyy-MM-dd HH:mm:ss");
            var sql = string.Format("CALL GetUsersOfRevisionsOfBefore({0}, '{1}')", filenameId, sqlFormattedDate);

            // TODO: place this in a custom filter
            var rawNames = Database.SqlQuery<string>(sql).ToList();
            var cleanNames = new HashSet<string>();

            foreach (var name in rawNames)
            {
                var cleanName = name.Replace("plus ", string.Empty);
                cleanName = cleanName.Replace("and the rest of the Xiph.Org Foundation", string.Empty);
                cleanName = cleanName.Replace(" and ", ",");
                if (cleanName.Contains(","))
                {
                    foreach (var cn in cleanName.Split(','))
                    {
                        cleanNames.Add(cn.Trim());
                    }
                }
                else
                    cleanNames.Add(cleanName.Trim());
            }

            return cleanNames.ToList();
        }

        public List<DeveloperForPath> GetDeveloperForPath(string path)
        {
            var sql = string.Format("CALL GetDevelopersForPath('{0}')", path + "%");

            return Database.SqlQuery<DeveloperForPath>(sql).ToList();
        }

        public List<DeveloperForPath> GetDeveloperWithoutPath()
        {
            return Database.SqlQuery<DeveloperForPath>("CALL GetDevelopersWOPath()").ToList();
        }

        public List<ActualReviewersGrouped> GetActualReviewersGrouped()
        {
            return Database.SqlQuery<ActualReviewersGrouped>("CALL GetActualReviewersGrouped()").ToList();
        }

        private IEnumerable<DeveloperExpertiseSum> GetDeveloperExpertiseSumForRepository(int repoditoryId, int numberOfHits = 0)
        {
            var sql = string.Format("CALL GetDeveloperExpertiseSum({0})", repoditoryId);

            var result = numberOfHits > 0 ? Database.SqlQuery<DeveloperExpertiseSum>(sql).Take(numberOfHits).ToList() : Database.SqlQuery<DeveloperExpertiseSum>(sql).ToList();

            return result;
        }
    }
}
