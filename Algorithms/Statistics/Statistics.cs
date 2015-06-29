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
                    List<string> uniqueReviewers = context.ComputedReviewers.Where(cr => cr.Bug.RepositoryId == RepositoryId).Select(cr => cr.Expert1).Distinct().ToList();
                    uniqueReviewers.AddRange(context.ComputedReviewers.Where(cr => cr.Bug.RepositoryId == RepositoryId).Select(cr => cr.Expert2).Distinct());
                    uniqueReviewers.AddRange(context.ComputedReviewers.Where(cr => cr.Bug.RepositoryId == RepositoryId).Select(cr => cr.Expert3).Distinct());
                    uniqueReviewers.AddRange(context.ComputedReviewers.Where(cr => cr.Bug.RepositoryId == RepositoryId).Select(cr => cr.Expert4).Distinct());
                    uniqueReviewers.AddRange(context.ComputedReviewers.Where(cr => cr.Bug.RepositoryId == RepositoryId).Select(cr => cr.Expert5).Distinct());

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

        public void AnalyzeActualReviews(SourceOfActualReviewers sourceOfActualReviewers)
        {
            ReadUniqueActualReviewers();
            ReadUniqueComputedReviewers();

            List<Author> actualReviewersAndAlternatives = Author.GetAuthorsFromFile(Path4ActualReviewers);
            List<Author> computedReviewersAndAlternatives = Author.GetAuthorsFromFile(Path4ComputedReviewers);

            List<int> algorithmIds;
            using (ExpertiseDBEntities context = new ExpertiseDBEntities())
                algorithmIds = context.Algorithms.Select(a => a.AlgorithmId).ToList();

            var output = algorithmIds.ToDictionary(algorithmId => algorithmId, algorithmId => new List<StatisticsResult>());

            IDictionary<int,string> actualReviewers = sourceOfActualReviewers.findReviewsWithReviewers();
            int count = 0;
            double elapsed = 0d;
            int maxCount = actualReviewers.Count();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            int errorCount = 0;

            foreach (int actualReviewerId in actualReviewers.Keys)
            {
                try
                {
                    if (count % 1000 == 0 && count > 0)
                    {
                        sw.Stop();
                        elapsed += sw.Elapsed.TotalSeconds;
                        double avg = elapsed / count;
                        TimeSpan remaining = TimeSpan.FromSeconds(avg * (maxCount - count));
                        log.DebugFormat("Now at: {0} - (act: {1} | avg: {2:N}s | remaining: {3})", count, sw.Elapsed, avg, remaining);
                        sw.Restart();
                    }

                    count++;

                    string actualReviewerName = actualReviewers[actualReviewerId].ToLowerInvariant();

                    IEnumerable<ComputedReviewer> computedReviewers = GetComputedReviewersForActualReviewerId(actualReviewerId);

                    Author alternativesForActualReviewer =
                        actualReviewersAndAlternatives.Single(
                            ar => ar.completeName.ToLowerInvariant() == actualReviewerName);

                    foreach (ComputedReviewer computedReviewer in computedReviewers)
                    {
                        var computedReviewerNamesAndValues = new List<KeyValuePair<string, double>>
                        {
                            new KeyValuePair<string, double>(computedReviewer.Expert1, computedReviewer.Expert1Value),
                            new KeyValuePair<string, double>(computedReviewer.Expert2, computedReviewer.Expert2Value),
                            new KeyValuePair<string, double>(computedReviewer.Expert3, computedReviewer.Expert3Value),
                            new KeyValuePair<string, double>(computedReviewer.Expert4, computedReviewer.Expert4Value),
                            new KeyValuePair<string, double>(computedReviewer.Expert5, computedReviewer.Expert5Value)
                        };

                        bool found = false;
                        for (int i = 0; i < 5; i++) // 5 is the number of reviewers that may be recommended in ComputedReviewers
                        {
                            if (computedReviewerNamesAndValues[i].Key == string.Empty)
                                break;

                            Author alternativesForComputedReviewer =
                            computedReviewersAndAlternatives.Single(
                                cr => cr.completeName.ToLowerInvariant() == computedReviewerNamesAndValues[i].Key.ToLowerInvariant());

                            if (!alternativesForActualReviewer.IsMatching(alternativesForComputedReviewer))
                                continue;

                            output[computedReviewer.AlgorithmId].Add(new StatisticsResult(actualReviewerId)
                                {
                                    AuthorWasExpertNo = i + 1,
                                    ExpertiseValue = computedReviewerNamesAndValues[i].Value
                                });

                            found = true;

                            break;
                        }

                        if (!found)
                            output[computedReviewer.AlgorithmId].Add(new StatisticsResult(actualReviewerId));
                    }
                }
                catch (Exception ex)
                {
                    if (++errorCount > 10)
                    {
                        log.Fatal("10 errors while computing statistics, ActualReviewerId=" + actualReviewerId + ", giving up.", ex);
                        throw new Exception("10 errors while computing statistics, giving up.", ex);
                    }

                    log.Error("Error #" + errorCount + " on ActualReviewerId " + actualReviewerId, ex);
                }
            }

            foreach (var algoStats in output)
            {
                var sb = new StringBuilder();
                foreach (StatisticsResult statisticsResult in algoStats.Value)
                {
                    sb.AppendLine(statisticsResult.ToCSV());
                }

                File.WriteAllText(string.Format(basepath + "stats_{0}.txt", algoStats.Key), sb.ToString());
            }
        }

        private IEnumerable<ComputedReviewer> GetComputedReviewersForActualReviewerId(int id)
        {
            using (var context = new ExpertiseDBEntities())
            {
                return context.ComputedReviewers.Where(cr => cr.ActualReviewerId == id).ToList().AsReadOnly();
            }
        }

        public void ComputeStatisticsForAllAlgorithmsAndActualReviews(SourceOfActualReviewers source)
        {
            List<int> algorithmIds;
            using (var context = new ExpertiseDBEntities())
                algorithmIds = context.Algorithms.Select(a => a.AlgorithmId).ToList();

            Parallel.ForEach(algorithmIds, algorithmId => ComputeStatisticsForAlgorithmAndActualReviews(algorithmId, source));
        }

        private void ComputeStatisticsForAlgorithmAndActualReviews(int algorithmId, SourceOfActualReviewers source)
        {
            string[] originalData = File.ReadAllLines(string.Format(basepath + "stats_{0}.txt", algorithmId));
            List<StatisticsResult> workingSet = originalData.Select(StatisticsResult.FromCSVLine)
                .Where(tmp => source.findReviews().Contains(tmp.ActualReviewerId)).ToList();

            int count = source.findReviews().Count();
            int foundNo = workingSet.Count(sr => sr.AuthorWasFound);
            int[] expertPlacements = new int[5];

            for (int i = 0; i < 5; i++)
            {
                expertPlacements[i] = workingSet.Count(sr => sr.AuthorWasExpertNo == (i + 1));
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Format("Expert was found: {0} / {1} ({2:P})", foundNo, count, (double)foundNo / count));
            for (int i = 0; i < 5; i++)
                sb.AppendLine(string.Format("Expert was No {0}:  {1} / {2} ({3:P})", i + 1, expertPlacements[i], count, (double)expertPlacements[i] / (double)count));

            File.WriteAllText(string.Format(basepath + "stats_{0}_analyzed{1}.txt", algorithmId, source.Postfix), sb.ToString());
        }

        /// <summary>
        /// computes the size of the set of entries that have the actual reviewer within the top 5 computed reviewers and is shared between all algorithms
        /// </summary>
        public void FindIntersectingEntriesForActualReviewerIds(SourceOfActualReviewers source)
        {
            IEnumerable<int> actualReviewerIds = source.findReviews();

            List<int> algorithmIds;
            using (var context = new ExpertiseDBEntities())
                algorithmIds = context.Algorithms.Select(a => a.AlgorithmId).ToList();

            log.Debug("Setting up");
            List<List<StatisticsResult>> allStatistics = algorithmIds
                    // read statistics for every algorithm
                .Select(algorithmId => File.ReadAllLines(string.Format(basepath + "stats_{0}.txt", algorithmId)))
                    // map the string[] with the statistics of each algorithm to a List<StatisticResult> for each algorithm
                .Select(originalData => originalData.Select(StatisticsResult.FromCSVLine).ToList()).ToList();

            List<StatisticsResult> workingSet = allStatistics[1]    // Why 1??? Maybe skip Line 10 rule, because it has at most one entry?
                .Where(statResult => actualReviewerIds.Contains(statResult.ActualReviewerId) && statResult.AuthorWasFound).ToList();
            int count = workingSet.Count;
            log.Info("Setup complete");

            for (int i = 2; i < algorithmIds.Count; i++)
            {
                log.InfoFormat("Now testing against {0}, working set count: {1}", i, workingSet.Count);
                workingSet = workingSet.Where(s => allStatistics[i].Any(stats => stats.ActualReviewerId == s.ActualReviewerId && stats.AuthorWasFound)).ToList();
            }

            string sb = string.Format("{0} / {1} intersecting entries", workingSet.Count, count);
            File.WriteAllText(string.Format(basepath + "stats{0}.txt", source.Postfix), sb);
        }

        /// <summary>
        /// computes the size of the set of entries that have the actual reviewer within the top 5 computed reviewers and is shared between two algorithms by pairwise comparison
        /// </summary>
        public void FindIntersectingEntriesPairwiseForActualReviewerIds(SourceOfActualReviewers source)
        {
            IEnumerable<int> actualReviewerIds = source.findReviews();
            List<int> algorithmIds;
            using (var context = new ExpertiseDBEntities())
                algorithmIds = context.Algorithms.Select(a => a.AlgorithmId).ToList();

            log.Debug("Setting up");
            var sb = new StringBuilder();
            List<List<StatisticsResult>> allStatistics = algorithmIds
                .Select(algorithmId => File.ReadAllLines(string.Format(basepath + "stats_{0}.txt", algorithmId)))
                    .Select(originalData => originalData.Select(StatisticsResult.FromCSVLine)
                    .ToList())
                .ToList();
            allStatistics[0] = allStatistics[0].Where(stat => stat.AuthorWasExpertNo == 1).ToList();
            log.Info("Setup complete");

            for (int i = 0; i < algorithmIds.Count; i++)
            {
                List<StatisticsResult> workingSet = allStatistics[i].Where(tmp => actualReviewerIds.Contains(tmp.ActualReviewerId) && tmp.AuthorWasFound).ToList();
                
                int count = workingSet.Count;
                for (int j = 0; j < algorithmIds.Count; j++)
                {
                    if (j == i)
                        continue;

                    List<StatisticsResult> result = workingSet.Where(s => allStatistics[j].Any(stats => stats.ActualReviewerId == s.ActualReviewerId && stats.AuthorWasFound)).ToList();

                    sb.AppendLine(string.Format("{0} / {1} ({2:P}) intersecting entries for A{3} and A{4}", result.Count, count, (double)result.Count / (double)count, i + 1, j + 1));
                }
            }

            File.WriteAllText(string.Format(basepath + "stats_intersect_pairwise{0}.txt", source.Postfix), sb.ToString());
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
