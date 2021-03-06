﻿namespace ExpertiseExplorer.ExpertiseDB
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Linq;

    using ExpertiseExplorer.ExpertiseDB.Extensions;
    using System.Globalization;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public partial class ExpertiseDBEntities
    {
        public ExpertiseDBEntities(string connectionString)
            : base(connectionString)
        {
            ((IObjectContextAdapter)this).ObjectContext.CommandTimeout = 240;
        }

        public int GetDeveloperIdFromNameForRepository(string name, int repositoryId)
        {
            Debug.Assert(!name.Contains(','));

            //name = prefilterAuthorName(name);
            
            //var names = name.Contains(',') ? name.Split(',') : new[] { name };

            //var result = new List<int>();
            //foreach (var n in names)
            //{
            //    var devName = n.Trim();
                var developer = Developers.SingleOrDefault(d => d.Name == name && d.RepositoryId == repositoryId);
                if (developer == null)
                {
                    developer = Developers.Add(new Developer { Name = name, RepositoryId = repositoryId });
                    SaveChanges();
                }

            //    result.Add(developer.DeveloperId);
            //}

            return developer.DeveloperId;
        }

        public List<Tuple<string, int, double>> GetTopDevelopersForRepository(int repositoryId, int numberOfHits = 0)
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

        public async Task<IEnumerable<SimplifiedDeveloperExpertise>> GetTop5DevelopersForArtifactsAndAlgorithm(IEnumerable<int> artifactIds, int algorithmId, Func<IEnumerable<double>,double> aggregateResults)
        {
            List<DeveloperExpertiseValue> lstAllResults = new List<DeveloperExpertiseValue>();
            IEnumerable<Task<List<DeveloperExpertiseValue>>> queryTasks =
                artifactIds
                .Select(async artId =>
                    await DeveloperExpertiseValues
                        .Include(de => de.DeveloperExpertise)
                        .Where(dev => dev.AlgorithmId == algorithmId && dev.DeveloperExpertise.ArtifactId == artId)
                        .ToListAsync()
                );   // this creates one SQL query for each artifact
            foreach(Task<List<DeveloperExpertiseValue>> tskQuery in queryTasks)     
                lstAllResults.AddRange(await tskQuery);

            return
                lstAllResults
                    //DeveloperExpertiseValues
                    //.Include(dev => dev.DeveloperExpertise.Developer).Include(de => de.DeveloperExpertise)
                    //.Where(dev => artifactIds.Contains(dev.DeveloperExpertise.ArtifactId) && dev.AlgorithmId == algorithmId)
                    //.AsNoTracking()

                    .GroupBy(
                        dev => dev.DeveloperExpertise.DeveloperId,
                        (devId, expertiseValues) => new SimplifiedDeveloperExpertise()
                            {
                                DeveloperId = devId,
                                Expertise = aggregateResults(expertiseValues.Select(exValue => exValue.Value))
                            })
                    .OrderByDescending(sde => sde.Expertise)
                    .Take(5);
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

        public DeveloperWithEditTime GetUserForLastRevisionOfBefore(int filenameId, DateTime before)
        {
            var sqlFormattedDate = before.ToString("yyyy-MM-dd HH:mm:ss");
            var sql = string.Format(CultureInfo.InvariantCulture, "CALL GetUserForLastRevisionOfBefore({0}, '{1}')", filenameId, sqlFormattedDate);
            DeveloperWithEditTime dev = Database.SqlQuery<DeveloperWithEditTime>(sql).SingleOrDefault();

            if (null == dev || string.IsNullOrEmpty(dev.User))
                return null;

            return dev;
        }

        public List<DeveloperWithEditTime> GetUsersOfRevisionsOfBefore(int filenameId, DateTime before)
        {
            var sqlFormattedDate = before.ToString("yyyy-MM-dd HH:mm:ss");
            var sql = string.Format(CultureInfo.InvariantCulture, "CALL GetUsersOfRevisionsOfBefore({0}, '{1}')", filenameId, sqlFormattedDate);

            return Database.SqlQuery<DeveloperWithEditTime>(sql).ToList();
        }

        public IEnumerable<DeveloperForPath> GetDevelopersForPath(int repositoryId, string path)
        {
            try
            {
                string sql = string.Format(CultureInfo.InvariantCulture, "CALL GetDevelopersForPath({0},'{1}')", repositoryId, path.Replace("'", "''") + "%");
                return Database.SqlQuery<DeveloperForPath>(sql).ToList();
            }
            catch (MySql.Data.MySqlClient.MySqlException mse) when (mse.InnerException is TimeoutException)
            {
                // this query can take quite a while. In this handler, we compute the result in single steps. This is slower, but
                // each partial query is less likely to produce a timeout

                List<int> similarArtifactIds = Artifacts
                    .Where(artifact => artifact.RepositoryId == repositoryId && artifact.Name.StartsWith(path))
                    .Select(artifact => artifact.ArtifactId)
                    .ToList();

                IDictionary<int,DeveloperForPath> allDevelopersByDeveloperId = new Dictionary<int,DeveloperForPath>();
                foreach (int artifactId in similarArtifactIds)
                {
                    IEnumerable<DeveloperForPath> relevantDeveloperExpertises = DeveloperExpertises
                        .Where(de => de.ArtifactId == artifactId)
                        .Select(de => new DeveloperForPath() {
                            DeveloperId = de.DeveloperId,
                            IsFirstAuthorCount = de.IsFirstAuthor ? 1 : 0,
                            DeliveriesCount = de.DeliveriesCount
                        })
                        .ToList(); // these are only partial results that have to be aggregated

                    foreach(DeveloperForPath dfp in relevantDeveloperExpertises)
                    {
                        if (allDevelopersByDeveloperId.ContainsKey(dfp.DeveloperId))
                        {
                            DeveloperForPath existingEntry = allDevelopersByDeveloperId[dfp.DeveloperId];
                            existingEntry.DeliveriesCount += dfp.DeliveriesCount;
                            existingEntry.IsFirstAuthorCount += dfp.IsFirstAuthorCount;
                        }
                        else
                            allDevelopersByDeveloperId[dfp.DeveloperId] = dfp;
                    }
                }

                return allDevelopersByDeveloperId.Values;
            }
        }

        public List<DeveloperForPath> GetDevelopersWithoutPath(int repositoryId)
        {
            string sql = string.Format(CultureInfo.InvariantCulture, "CALL GetDevelopersWOPath({0})", repositoryId);

            return Database.SqlQuery<DeveloperForPath>(sql).ToList();
        }

        public List<ActualReviewersGrouped> GetActualReviewersGrouped(int repositoryId)
        {
            string sql = string.Format(CultureInfo.InvariantCulture, "CALL GetActualReviewersGrouped({0})", repositoryId);

            return Database.SqlQuery<ActualReviewersGrouped>(sql).ToList();
        }

        private IEnumerable<DeveloperExpertiseSum> GetDeveloperExpertiseSumForRepository(int repositoryId, int numberOfHits = 0)
        {
            var sql = string.Format(CultureInfo.InvariantCulture,"CALL GetDeveloperExpertiseSum({0})", repositoryId);

            var result = numberOfHits > 0 ? Database.SqlQuery<DeveloperExpertiseSum>(sql).Take(numberOfHits).ToList() : Database.SqlQuery<DeveloperExpertiseSum>(sql).ToList();

            return result;
        }

        public void StoreDeveloperExpertiseValue(string developerName, double expertiseValue, int artifactId, int repositoryId, int algorithmId)
        {
            string sql = string.Format(CultureInfo.InvariantCulture,"CALL StoreDeveloperExpertiseValue('{0}',{1},{2},{3},{4})", developerName.Replace("'","''"), expertiseValue, artifactId, repositoryId, algorithmId);
            Database.ExecuteSqlCommand(sql);
        }
    }
}
