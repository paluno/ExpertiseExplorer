namespace ExpertiseExplorer.Algorithms.Statistics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ExpertiseDB;
    using ExpertiseExplorer.Algorithms.RepositoryManagement;
    using log4net;

    public class Statistics
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string repositoryURL;
        private readonly string basepath;

        private string Path4ActualReviewers { get { return basepath + "reviewers_actual.txt"; } }
        private string Path4ComputedReviewers { get { return basepath + "reviewers_computed.txt"; } }
        private string Path4MissingReviewers { get { return basepath + "reviewers_missing.txt"; } }

        private int _RepositoryId = int.MinValue;
        public int RepositoryId
        {
            get
            {
                if (int.MinValue == _RepositoryId)
                {
                    using (ExpertiseDBEntities entities = new ExpertiseDBEntities())
                    {
                        _RepositoryId = entities.Repositorys.Single(repository => repository.SourceURL == repositoryURL).RepositoryId;
                    }
                }

                return _RepositoryId;
            }
        }

        public Statistics(string sourceURL, string basepath)
        {
            this.basepath = basepath;
            this.repositoryURL = sourceURL;
        }

        #region Common helper methods
        public void ReadUniqueActualReviewers()
        {
            if (!File.Exists(Path4ActualReviewers))
                using (var context = new ExpertiseDBEntities())
                {
                    var uniqueReviewers = context.ActualReviewers.Where(ar => ar.Bug.RepositoryId == RepositoryId).Select(ar => ar.Reviewer).Distinct().OrderBy(ar => ar).ToList();

                    var sb = new StringBuilder();
                    foreach (var uniqueReviewer in uniqueReviewers)
                    {
                        sb.AppendLine(uniqueReviewer);
                    }

                    File.WriteAllText(Path4ActualReviewers, sb.ToString());
                }
        }

        public void ReadUniqueComputedReviewers()
        {
            if (!File.Exists(Path4ComputedReviewers))
                using (var context = new ExpertiseDBEntities())
                {
                    List<string> uniqueReviewers = context.ComputedReviewers.Where(cr => cr.Bug.RepositoryId == RepositoryId).Select(cr => cr.Expert1.Name).Distinct().ToList();
                    uniqueReviewers.AddRange(context.ComputedReviewers.Where(cr => cr.Bug.RepositoryId == RepositoryId).Select(cr => cr.Expert2.Name).Distinct());
                    uniqueReviewers.AddRange(context.ComputedReviewers.Where(cr => cr.Bug.RepositoryId == RepositoryId).Select(cr => cr.Expert3.Name).Distinct());
                    uniqueReviewers.AddRange(context.ComputedReviewers.Where(cr => cr.Bug.RepositoryId == RepositoryId).Select(cr => cr.Expert4.Name).Distinct());
                    uniqueReviewers.AddRange(context.ComputedReviewers.Where(cr => cr.Bug.RepositoryId == RepositoryId).Select(cr => cr.Expert5.Name).Distinct());

                    uniqueReviewers = uniqueReviewers.Distinct().OrderBy(cr => cr).ToList();

                    var sb = new StringBuilder();
                    foreach (var uniqueReviewer in uniqueReviewers)
                    {
                        sb.AppendLine(uniqueReviewer);
                    }

                    File.WriteAllText(Path4ComputedReviewers, sb.ToString());
                }
        }

        private string GetAuthorsWhoAreNotInDb(IEnumerable<Author> input)
        {
            List<Author> result = new List<Author>();
            List<string> developernames;
            using (ExpertiseDBEntities context = new ExpertiseDBEntities())
            {
                developernames = context.Developers.Where(d => d.RepositoryId == RepositoryId).Select(d => d.Name).ToList();
            }

            foreach (Author author in input)
            {
                string toFind = author.completeName.ToLowerInvariant();
                bool found = developernames.Any(d => d.ToLowerInvariant().Contains(toFind));

                if (found)
                    continue;

                bool isFound = author.Alternatives.Any(alternative => developernames.Any(d => d.ToLowerInvariant().Contains(alternative.completeName.ToLowerInvariant())));

                if (!isFound)
                    result.Add(author);
            }

            StringBuilder sb = new StringBuilder();
            foreach (var missingReviewer in result)
                sb.AppendLine(
                    missingReviewer.completeName + ";" + string.Join(";", missingReviewer.Alternatives.Select(a => a.completeName)));

            return sb.ToString();
        }
        #endregion Common helper methods

        #region Find Missing Reviewers
        public void FindMissingReviewers()
        {
            ReadUniqueActualReviewers();
            ReadUniqueComputedReviewers();
            ReadMissingReviewers();
            List<Author> missing = Author.GetAuthorsFromFile(Path4MissingReviewers);
            string missingReviewers = GetAuthorsWhoAreNotInDb(missing);
            File.WriteAllText(basepath + "reviewers_missing_in_db.txt", missingReviewers);
        }

        public void ReadMissingReviewers()
        {
            List<Author> actualReviewers = Author.GetAuthorsFromFile(Path4ActualReviewers);
            List<Author> computedReviewers = Author.GetAuthorsFromFile(Path4ComputedReviewers);

            List<Author> missingReviewers = new List<Author>();
            foreach (Author actualReviewer in actualReviewers)
            {
                // first the reviewer
                if (computedReviewers.Any(cr => cr.completeName.ToLowerInvariant().Contains(actualReviewer.completeName.ToLowerInvariant())))
                    continue;

                // second the alternatives
                bool found = actualReviewer.Alternatives.Any(alternative => computedReviewers.Any(cr => cr.completeName.ToLowerInvariant().Contains(alternative.completeName.ToLowerInvariant())));

                if (found)
                    continue;

                // only add if no alternative was already added
                bool isIn = actualReviewer.Alternatives.Any(alternative => missingReviewers.Any(mr => mr.completeName == alternative.completeName));

                if (!isIn)
                    missingReviewers.Add(actualReviewer);
            }

            StringBuilder sb = new StringBuilder();
            foreach (Author missingReviewer in missingReviewers)
                sb.AppendLine(
                    missingReviewer.completeName + ";" + string.Join(";", missingReviewer.Alternatives.Select(a => a.completeName)));

            File.WriteAllText(Path4MissingReviewers, sb.ToString());
        }
#endregion Find Missing Reviewers

        /// <summary>
        /// Iterates through the list of bugs and checks for each bug which algorithms suggested correct reviewers and which did not.
        /// Results are written to stats_x.txt: A CSV with bugId and correctly predicted reviewers in each entry (StatisticsResult strings)
        /// </summary>
        public void AnalyzeActualReviews(SourceOfActualReviewers sourceOfActualReviewers)
        {
            IEnumerable<int> allBugIds = sourceOfActualReviewers.findBugs();

            int someRandomBugId = allBugIds.First();
            List<int> algorithmIds;
            using (ExpertiseDBEntities context = new ExpertiseDBEntities())
                algorithmIds = context.Algorithms
                    .Select(a => a.AlgorithmId)
                    .Where(algoId => context.ComputedReviewers.Any(cr => cr.BugId == someRandomBugId && cr.AlgorithmId == algoId))    // filter algorithms for which no calculation has been done
                    .ToList();


            int count = 0;
            int errorCount = 0;
            double elapsed = 0d;
            int maxCount = allBugIds.Count();
            Stopwatch sw = new Stopwatch();
            sw.Start();

            IDictionary<int, List<StatisticsResult>> output = algorithmIds.ToDictionary(algorithmId => algorithmId, algorithmId => new List<StatisticsResult>());
            foreach (int bugId in allBugIds)
            {
                if (++count % 1000 == 0 && count > 0)
                {
                    sw.Stop();
                    elapsed += sw.Elapsed.TotalSeconds;
                    double avg = elapsed / count;
                    TimeSpan remaining = TimeSpan.FromSeconds(avg * (maxCount - count));
                    log.DebugFormat("Now at: {0} - (act: {1} | avg: {2:N}s | remaining: {3})", count, sw.Elapsed, avg, remaining);
                    sw.Restart();
                }

                try
                {
                    List<int> actualReviewerIds;
                    using (ExpertiseDBEntities context = new ExpertiseDBEntities())
                        actualReviewerIds = context.ActualReviewers
                            .Where(ar => ar.BugId == bugId)
                            .Select(ar => context.Developers.FirstOrDefault(dev => dev.Name == ar.Reviewer && dev.RepositoryId == RepositoryId).DeveloperId)
                            .ToList();

                    Debug.Assert(actualReviewerIds.Count > 0);  // All bugs must have reviewers

                    foreach (int algorithmId in algorithmIds)
                        output[algorithmId].Add(CalculateResultForOneAlgorithmAndBug(algorithmId, bugId, actualReviewerIds));
                }
                catch (Exception ex)
                {
                    if (++errorCount > 10)
                    {
                        log.Fatal("10 errors while computing statistics, BugID=" + bugId + ", giving up.", ex);
                        throw new Exception("10 errors while computing statistics, giving up.", ex);
                    }

                    log.Error("Error #" + errorCount + " on BugID " + bugId, ex);
                }
            }

            foreach (int algorithmId in output.Keys)
            {
                List<StatisticsResult> algoStats = output[algorithmId];
                var sb = new StringBuilder();
                foreach (StatisticsResult statisticsResult in algoStats)
                {
                    sb.AppendLine(statisticsResult.ToCSV());
                }

                File.WriteAllText(string.Format(basepath + "stats_{0}.txt", algorithmId), sb.ToString());
            }
        }

        private StatisticsResult CalculateResultForOneAlgorithmAndBug(int algorithmId, int bugId, List<int> actualReviewerIds)
        {
            StatisticsResult sr = new StatisticsResult(bugId);
            ComputedReviewer currentReviewerSet;
            using (ExpertiseDBEntities context = new ExpertiseDBEntities())
            {
                currentReviewerSet = context.ComputedReviewers.Single(cr => cr.AlgorithmId == algorithmId && cr.BugId == bugId);
            }

            sr.Matches[0] = actualReviewerIds.Contains(currentReviewerSet.Expert1Id ?? -1); // the magic "-1" never matches
            sr.Matches[1] = actualReviewerIds.Contains(currentReviewerSet.Expert2Id ?? -1);
            sr.Matches[2] = actualReviewerIds.Contains(currentReviewerSet.Expert3Id ?? -1);
            sr.Matches[3] = actualReviewerIds.Contains(currentReviewerSet.Expert4Id ?? -1);
            sr.Matches[4] = actualReviewerIds.Contains(currentReviewerSet.Expert5Id ?? -1);

            return sr;
        }

        /// <summary>
        /// Expects stats_x.txt entries for all algorithms. It then counts the hits and misses and computes
        /// the fraction of hits. Results are written to stats_x_analyzedPOSTFIX.txt.
        /// </summary>
        public void ComputeStatisticsForAllAlgorithmsAndActualReviews(SourceOfActualReviewers source)
        {
            List<int> algorithmIds;
            using (var context = new ExpertiseDBEntities())
                algorithmIds = context.Algorithms
                    .Select(a => a.AlgorithmId).ToList()        // perform the SQL query
                    .Where(algoID => File.Exists($"{basepath}stats_{algoID}.txt")).ToList();    // check whether the file exists on disk
            Debug.Assert(algorithmIds.Any());

            Parallel.ForEach(algorithmIds, algorithmId => ComputeStatisticsForAlgorithmAndActualReviews(algorithmId, source));
        }

        private void ComputeStatisticsForAlgorithmAndActualReviews(int algorithmId, SourceOfActualReviewers source)
        {
            string[] originalData = File.ReadAllLines($"{basepath}stats_{algorithmId}.txt");
            List<StatisticsResult> workingSet = originalData.Select(StatisticsResult.FromCSVLine)
                .Where(sr => source.findBugs().Contains(sr.BugId)).ToList();

            int count = workingSet.Count();
            int foundNo = workingSet.Count(sr => sr.IsMatch);
            int[] expertPlacements = new int[StatisticsResult.NUMBER_OF_EXPERTS];

            for (int i = 0; i < StatisticsResult.NUMBER_OF_EXPERTS; i++)
            {
                expertPlacements[i] = workingSet.Count(sr => sr.Matches[i]);
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Expert was found: {foundNo} / {count} ({(double)foundNo / count:P})");
            for (int i = 0; i < StatisticsResult.NUMBER_OF_EXPERTS; i++)
                sb.AppendLine($"Expert was No {i+1}:  {expertPlacements[i]} / {count} ({(double)expertPlacements[i] / (double)count:P})");

            File.WriteAllText($"{basepath}stats_{algorithmId}_analyzed{source.Postfix}.txt", sb.ToString());
        }

        /// <summary>
        /// computes the size of the set of entries that have the actual reviewer within the top 5 computed reviewers and is shared between all algorithms
        /// </summary>
        public void FindIntersectingEntriesForActualReviewerIds(SourceOfActualReviewers source)
        {
            throw new NotImplementedException("This must be reimplemented to reflect the changes to StatisticsResult");

            //IEnumerable<int> actualReviewerIds = source.findBugs();

            //List<int> algorithmIds;
            //using (var context = new ExpertiseDBEntities())
            //    algorithmIds = context.Algorithms.Select(a => a.AlgorithmId).ToList();

            //log.Debug("Setting up");
            //List<List<StatisticsResult>> allStatistics = algorithmIds
            //        // read statistics for every algorithm
            //    .Select(algorithmId => File.ReadAllLines(string.Format(basepath + "stats_{0}.txt", algorithmId)))
            //        // map the string[] with the statistics of each algorithm to a List<StatisticResult> for each algorithm
            //    .Select(originalData => originalData.Select(StatisticsResult.FromCSVLine).ToList()).ToList();

            //List<StatisticsResult> workingSet = allStatistics[1]    // Why 1??? Maybe skip Line 10 rule, because it has at most one entry?
            //    .Where(statResult => actualReviewerIds.Contains(statResult.ActualReviewerId) && statResult.AuthorWasFound).ToList();
            //int count = workingSet.Count;
            //log.Info("Setup complete");

            //for (int i = 2; i < algorithmIds.Count; i++)
            //{
            //    log.InfoFormat("Now testing against {0}, working set count: {1}", i, workingSet.Count);
            //    workingSet = workingSet.Where(s => allStatistics[i].Any(stats => stats.ActualReviewerId == s.ActualReviewerId && stats.AuthorWasFound)).ToList();
            //}

            //string sb = string.Format("{0} / {1} intersecting entries", workingSet.Count, count);
            //File.WriteAllText(string.Format(basepath + "stats{0}.txt", source.Postfix), sb);
        }

        /// <summary>
        /// computes the size of the set of entries that have the actual reviewer within the top 5 computed reviewers and is shared between two algorithms by pairwise comparison
        /// </summary>
        public void FindIntersectingEntriesPairwiseForActualReviewerIds(SourceOfActualReviewers source)
        {
            throw new NotImplementedException("This must be reimplemented to reflect the changes to StatisticsResult");

            //IEnumerable<int> actualReviewerIds = source.findBugs();
            //List<int> algorithmIds;
            //using (var context = new ExpertiseDBEntities())
            //    algorithmIds = context.Algorithms.Select(a => a.AlgorithmId).ToList();

            //log.Debug("Setting up");
            //var sb = new StringBuilder();
            //List<List<StatisticsResult>> allStatistics = algorithmIds
            //    .Select(algorithmId => File.ReadAllLines(string.Format(basepath + "stats_{0}.txt", algorithmId)))
            //        .Select(originalData => originalData.Select(StatisticsResult.FromCSVLine)
            //        .ToList())
            //    .ToList();
            //allStatistics[0] = allStatistics[0].Where(stat => stat.AuthorWasExpertNo == 1).ToList();
            //log.Info("Setup complete");

            //for (int i = 0; i < algorithmIds.Count; i++)
            //{
            //    List<StatisticsResult> workingSet = allStatistics[i].Where(tmp => actualReviewerIds.Contains(tmp.ActualReviewerId) && tmp.AuthorWasFound).ToList();
                
            //    int count = workingSet.Count;
            //    for (int j = 0; j < algorithmIds.Count; j++)
            //    {
            //        if (j == i)
            //            continue;

            //        List<StatisticsResult> result = workingSet.Where(s => allStatistics[j].Any(stats => stats.ActualReviewerId == s.ActualReviewerId && stats.AuthorWasFound)).ToList();

            //        sb.AppendLine(string.Format("{0} / {1} ({2:P}) intersecting entries for A{3} and A{4}", result.Count, count, (double)result.Count / (double)count, i + 1, j + 1));
            //    }
            //}

            //File.WriteAllText(string.Format(basepath + "stats_intersect_pairwise{0}.txt", source.Postfix), sb.ToString());
        }

        public void FindAliases(AliasFinder af)
        {
            ReadUniqueActualReviewers();
            IEnumerable<string> actualReviewers = af.Consolidate(File.ReadAllLines(Path4ActualReviewers))
                 .Select(reviewerList => string.Join(",",reviewerList))     // put each reviewer in one string
                 .OrderBy(x => x);                                          // sort the resulting reviewers
            using (TextWriter writerActualReviewers = new StreamWriter(Path4ActualReviewers))
                foreach (string oneReviewer in actualReviewers)
                    writerActualReviewers.WriteLine(oneReviewer);
            
            ReadUniqueComputedReviewers();
            IEnumerable<string> computedReviewers = af.Consolidate(File.ReadAllLines(Path4ComputedReviewers))
                 .Select(reviewerList => string.Join(",", reviewerList))     // put each reviewer in one string
                 .OrderBy(x => x);                                           // sort the resulting reviewers
            using (TextWriter writerComputedReviewers = new StreamWriter(Path4ComputedReviewers))
                foreach (string oneReviewer in computedReviewers)
                    writerComputedReviewers.WriteLine(oneReviewer);
        }

        public void FindAliasesFromNames(string path2NamesFile)
        {
            AliasFinder af = new AliasFinder();
            af.InitializeMappingFromNames(File.ReadAllLines(path2NamesFile));
            FindAliases(af);
        }

        public void FindAliasesFromAuthors(string path2AuthorFile)
        {
            AliasFinder af = new AliasFinder();
            af.InitializeMappingFromAuthorList(File.ReadAllLines(path2AuthorFile));
            FindAliases(af);
        }
    }
}
