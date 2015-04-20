namespace Statistics
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using ExpertiseDB;

    internal class Statistics
    {
        private readonly string basepath;

        public Statistics(string basepath)
        {
            this.basepath = basepath;
        }

        public void Run(StatisticsOperation statisticsOperation, StatisticsSource statisticsSource = StatisticsSource.None)
        {
            // sources: all, w/o hg, only one artifact
            IEnumerable<int> ids = new List<int>();
            var postfix = string.Empty;

            switch (statisticsSource)
            {
                case StatisticsSource.All:
                    using (var context = new ExpertiseDBEntities())
                    {
                        ids = context.ActualReviewers.Select(ar => ar.ActualReviewerId).ToList();
                    }

                    break;

                case StatisticsSource.WithoutHg:
                    ids = GetReviewsWithoutHg();
                    postfix = "_wo_hg";
                    break;

                case StatisticsSource.OnlyOneArtifact:
                    ids = GetReviewsWithOnlyOneArtifact();
                    postfix = "_only_one";
                    break;
            }

            switch (statisticsOperation)
            {
                case StatisticsOperation.FindMissingReviewers:
                    ReadUniqueActualReviewers();
                    ReadUniqueComputedReviewers();
                    ReadMissingReviewers();
                    var missing = GetAuthorsFromFile(basepath + "reviewers_missing.txt");
                    var missingReviewers = GetAuthorsWhoAreNotInDb(missing);
                    File.WriteAllText(basepath + "reviewers_missing_in_db.txt", missingReviewers);
                    break;

                case StatisticsOperation.AnalyzeActualReviews:
                    ReadUniqueActualReviewers();
                    ReadUniqueComputedReviewers();
                    AnalyzeActualReviews(ids);
                    break;

                case StatisticsOperation.ComputeStatisticsForAllAlgorithmsAndActualReviews:
                    ComputeStatisticsForAllAlgorithmsAndActualReviews(ids, postfix);
                    break;

                case StatisticsOperation.FindIntersectingEntriesForAllAlgorithms:
                    FindIntersectingEntriesForActualReviewerIds(ids, postfix);
                    break;

                case StatisticsOperation.FindIntersectingEntriesForAllAlgorithmsPairwise:
                    FindIntersectingEntriesPairwiseForActualReviewerIds(ids, postfix);
                    break;

                default:
                    throw new ArgumentOutOfRangeException("statisticsOperation");
            }
        }

        public void ReadUniqueActualReviewers()
        {
            using (var context = new ExpertiseDBEntities())
            {
                var uniqueReviewers = context.ActualReviewers.Select(ar => ar.Reviewer).Distinct().OrderBy(ar => ar).ToList();

                var sb = new StringBuilder();
                foreach (var uniqueReviewer in uniqueReviewers)
                {
                    sb.AppendLine(uniqueReviewer);
                }

                File.WriteAllText(basepath + "reviewers_actual.txt", sb.ToString());
            }
        }

        public void ReadUniqueComputedReviewers()
        {
            using (var context = new ExpertiseDBEntities())
            {
                var uniqueReviewers = context.ComputedReviewers.Select(cr => cr.Expert1).Distinct().ToList();
                uniqueReviewers.AddRange(context.ComputedReviewers.Select(cr => cr.Expert2).Distinct());
                uniqueReviewers.AddRange(context.ComputedReviewers.Select(cr => cr.Expert3).Distinct());
                uniqueReviewers.AddRange(context.ComputedReviewers.Select(cr => cr.Expert4).Distinct());
                uniqueReviewers.AddRange(context.ComputedReviewers.Select(cr => cr.Expert5).Distinct());

                uniqueReviewers = uniqueReviewers.Distinct().OrderBy(cr => cr).ToList();

                var sb = new StringBuilder();
                foreach (var uniqueReviewer in uniqueReviewers)
                {
                    sb.AppendLine(uniqueReviewer);
                }

                File.WriteAllText(basepath + "reviewers_computed.txt", sb.ToString());
            }
        }

        public void ReadMissingReviewers()
        {
            var actualReviewers = GetAuthorsFromFile(basepath + "reviewers_actual.txt");
            var computedReviewers = GetAuthorsFromFile(basepath + "reviewers_computed.txt");

            var missingReviewers = new List<Author>();
            foreach (var actualReviewer in actualReviewers)
            {
                // first the reviewer
                if (computedReviewers.FirstOrDefault(cr => cr.Name.ToLowerInvariant().Contains(actualReviewer.Name.ToLowerInvariant())) != null)
                    continue;

                // second the alternatives
                var found = actualReviewer.Alternatives.Any(alternative => computedReviewers.FirstOrDefault(cr => cr.Name.ToLowerInvariant().Contains(alternative.Name.ToLowerInvariant())) != null);

                if (found)
                    continue;

                // only add if no alternative was already added
                var isIn = actualReviewer.Alternatives.Any(alternative => missingReviewers.FirstOrDefault(mr => mr.Name == alternative.Name) != null);

                if (!isIn)
                    missingReviewers.Add(actualReviewer);
            }

            var sb = new StringBuilder();
            foreach (var missingReviewer in missingReviewers)
                sb.AppendLine(
                    missingReviewer.Name + ";" + string.Join(";", missingReviewer.Alternatives.Select(a => a.Name)));

            File.WriteAllText(basepath + "reviewers_missing.txt", sb.ToString());
        }

        public List<Author> GetAuthorsFromFile(string file)
        {
            var authorLines = File.ReadAllLines(file);

            var result = new List<Author>();

            foreach (var authorLine in authorLines)
                result.AddRange(GetAuthorsFromLine(authorLine));

            return result;
        }

        private string GetAuthorsWhoAreNotInDb(IEnumerable<Author> input)
        {
            var result = new List<Author>();
            List<string> developernames;
            using (var contex = new ExpertiseDBEntities())
            {
                developernames = contex.Developers.Select(d => d.Name).ToList();
            }

            foreach (var author in input)
            {
                var toFind = author.Name.ToLowerInvariant();
                var found = developernames.FirstOrDefault(d => d.ToLowerInvariant().Contains(toFind)) != null;

                if (found)
                    continue;

                var isFound = author.Alternatives.Any(alternative => developernames.FirstOrDefault(d => d.ToLowerInvariant().Contains(alternative.Name.ToLowerInvariant())) != null);

                if (!isFound)
                    result.Add(author);
            }

            var sb = new StringBuilder();
            foreach (var missingReviewer in result)
                sb.AppendLine(
                    missingReviewer.Name + ";" + string.Join(";", missingReviewer.Alternatives.Select(a => a.Name)));

            return sb.ToString();
        }

        private void AnalyzeActualReviews(IEnumerable<int> actualReviewerIds)
        {
            var actualReviewersAndAlternatives = GetAuthorsFromFile(basepath + "reviewers_actual.txt");
            var computedReviewersAndAlternatives = GetAuthorsFromFile(basepath + "reviewers_computed.txt");

            List<int> algorithmIds;
            using (var context = new ExpertiseDBEntities())
                algorithmIds = context.Algorithms.Select(a => a.AlgorithmId).ToList();

            var output = algorithmIds.ToDictionary(algorithmId => algorithmId, algorithmId => new List<StatisticsResult>());

            var count = 0;
            var elapsed = 0d;
            var maxCount = actualReviewerIds.Count();
            var sw = new Stopwatch();
            sw.Start();
            foreach (var actualReviewerId in actualReviewerIds)
            {
                if (count % 1000 == 0 && count > 0)
                {
                    sw.Stop();
                    elapsed += sw.Elapsed.TotalSeconds;
                    var avg = elapsed / count;
                    var remaining = TimeSpan.FromSeconds(avg * (maxCount - count));
                    Console.WriteLine("Now at: {0} - (act: {1} | avg: {2:N}s | remaining: {3})", count, sw.Elapsed, avg, remaining);
                    sw.Restart();
                }

                count++;

                ActualReviewer actualReviewer;
                using (var context = new ExpertiseDBEntities())
                {
                    actualReviewer = context.ActualReviewers.Find(actualReviewerId);
                }

                var computedReviewers = GetComputedReviewersForActualReviewerId(actualReviewerId);

                var alternativesForActualReviewer =
                    actualReviewersAndAlternatives.Single(
                        ar => ar.Name.ToLowerInvariant() == actualReviewer.Reviewer.ToLowerInvariant());

                foreach (var computedReviewer in computedReviewers)
                {
                    var computedReviewerNamesAndValues = new List<KeyValuePair<string, double>>
                        {
                            new KeyValuePair<string, double>(computedReviewer.Expert1, computedReviewer.Expert1Value),
                            new KeyValuePair<string, double>(computedReviewer.Expert2, computedReviewer.Expert2Value),
                            new KeyValuePair<string, double>(computedReviewer.Expert3, computedReviewer.Expert3Value),
                            new KeyValuePair<string, double>(computedReviewer.Expert4, computedReviewer.Expert4Value),
                            new KeyValuePair<string, double>(computedReviewer.Expert5, computedReviewer.Expert5Value)
                        };

                    var found = false;
                    for (var i = 0; i < 5; i++)
                    {
                        if (computedReviewerNamesAndValues[i].Key == string.Empty)
                            break;

                        var alternativesForComputedReviewer =
                        computedReviewersAndAlternatives.Single(
                            cr => cr.Name.ToLowerInvariant() == computedReviewerNamesAndValues[i].Key.ToLowerInvariant());

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

            foreach (var algoStats in output)
            {
                var sb = new StringBuilder();
                foreach (var statisticsResult in algoStats.Value)
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

        private IEnumerable<Author> GetAuthorsFromLine(string line)
        {
            var names = line.Split(';');
            var tmpAuthors = names.Select(name => new Author { Name = name }).ToList();

            var result = new List<Author>();
            for (var i = 0; i < names.Length; i++)
            {
                var currentAuthor = tmpAuthors[i];

                for (var j = 0; j < names.Length; j++)
                {
                    if (i == j)
                        continue;

                    currentAuthor.Alternatives.Add(tmpAuthors[j]);
                }

                result.Add(currentAuthor);
            }

            return result;
        }

        // filters Mozilla's original import "author" hg@mozilla.com
        private IEnumerable<int> GetReviewsWithoutHg()
        {
            using (var context = new ExpertiseDBEntities())
            {
                var tmp = context.ComputedReviewers.Where(cr => cr.Expert1 != "hg@mozilla.com" && cr.Expert2 != "hg@mozilla.com" && cr.Expert3 != "hg@mozilla.com" && cr.Expert4 != "hg@mozilla.com" && cr.Expert5 != "hg@mozilla.com").Select(
                        cr => cr.ActualReviewerId).Distinct().ToList();

                var result = tmp.Where(id => !context.ComputedReviewers.Where(cr => cr.ActualReviewerId == id).Any(cr => cr.Expert1 == "hg@mozilla.com" || cr.Expert2 == "hg@mozilla.com" || cr.Expert3 == "hg@mozilla.com" || cr.Expert4 == "hg@mozilla.com" || cr.Expert5 == "hg@mozilla.com")).ToList();

                return result;
            }
        }

        private IEnumerable<int> GetReviewsWithOnlyOneArtifact()
        {
            var result = new List<int>();

            using (var context = new ExpertiseDBEntities())
            {
                var actualReviewersGrouped = context.GetActualReviewersGrouped();
                actualReviewersGrouped = actualReviewersGrouped.Where(arg => arg.Count == 1).ToList();

                result.AddRange(from reviewersGrouped in actualReviewersGrouped where reviewersGrouped.Count == 1 select context.ActualReviewers.First(ar => ar.ChangeId == reviewersGrouped.ChangeId).ActualReviewerId);
            }

            return result;
        }

        private void ComputeStatisticsForAllAlgorithmsAndActualReviews(IEnumerable<int> actualReviewerIds, string postfix = "")
        {
            List<int> algorithmIds;
            using (var context = new ExpertiseDBEntities())
                algorithmIds = context.Algorithms.Select(a => a.AlgorithmId).ToList();

            Parallel.ForEach(algorithmIds, algorithmId => ComputeStatisticsForAlgorithmAndActualReviews(algorithmId, actualReviewerIds, postfix));
        }

        private void ComputeStatisticsForAlgorithmAndActualReviews(int algorithId, IEnumerable<int> actualReviewerIds, string postfix)
        {
            string[] originalData = File.ReadAllLines(string.Format(basepath + "stats_{0}.txt", algorithId));
            List<StatisticsResult> workingSet = originalData.Select(StatisticsResult.FromCSVLine).Where(tmp => actualReviewerIds.Contains(tmp.ActualReviewerId)).ToList();

            int count = actualReviewerIds.Count();
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

            File.WriteAllText(string.Format(basepath + "stats_{0}_analyzed{1}.txt", algorithId, postfix), sb.ToString());
        }

        // computes the size of the set of entries that have the actual reviewer within the top 5 computed reviewers and is shared between all algorithms
        private void FindIntersectingEntriesForActualReviewerIds(IEnumerable<int> actualReviewerIds, string postfix = "")
        {
            List<int> algorithmIds;
            using (var context = new ExpertiseDBEntities())
                algorithmIds = context.Algorithms.Select(a => a.AlgorithmId).ToList();

            Console.WriteLine("Setting up");
            var allStatistics = algorithmIds.Select(algorithmId => File.ReadAllLines(string.Format(basepath + "stats_{0}.txt", algorithmId))).Select(originalData => originalData.Select(StatisticsResult.FromCSVLine).ToList()).ToList();
            var workingSet = allStatistics[1].Where(tmp => actualReviewerIds.Contains(tmp.ActualReviewerId) && tmp.AuthorWasFound).ToList();
            var count = workingSet.Count;
            Console.WriteLine("Setup complete");

            for (var i = 2; i < algorithmIds.Count; i++)
            {
                Console.WriteLine("Now testing against {0} , working set count: {1}", i, workingSet.Count);
                workingSet = workingSet.Where(s => allStatistics[i].Any(stats => stats.ActualReviewerId == s.ActualReviewerId && stats.AuthorWasFound)).ToList();
            }

            var sb = string.Format("{0} / {1} intersecting entries", workingSet.Count, count);
            File.WriteAllText(string.Format(basepath + "stats{0}.txt", postfix), sb);
        }

        // computes the size of the set of entries that have the actual reviewer within the top 5 computed reviewers and is shared between two algorithms by pairwise comparison
        private void FindIntersectingEntriesPairwiseForActualReviewerIds(IEnumerable<int> actualReviewerIds, string postfix)
        {
            List<int> algorithmIds;
            using (var context = new ExpertiseDBEntities())
                algorithmIds = context.Algorithms.Select(a => a.AlgorithmId).ToList();

            Console.WriteLine("Setting up");
            var sb = new StringBuilder();
            var allStatistics = algorithmIds.Select(algorithmId => File.ReadAllLines(string.Format(basepath + "stats_{0}.txt", algorithmId))).Select(originalData => originalData.Select(StatisticsResult.FromCSVLine).ToList()).ToList();
            allStatistics[0] = allStatistics[0].Where(stat => stat.AuthorWasExpertNo == 1).ToList();
            Console.WriteLine("Setup complete");

            for (var i = 0; i < algorithmIds.Count; i++)
            {
                var workingSet = allStatistics[i].Where(tmp => actualReviewerIds.Contains(tmp.ActualReviewerId) && tmp.AuthorWasFound).ToList();
                
                var count = workingSet.Count;
                for (var j = 0; j < algorithmIds.Count; j++)
                {
                    if (j == i)
                        continue;

                    var result = workingSet.Where(s => allStatistics[j].Any(stats => stats.ActualReviewerId == s.ActualReviewerId && stats.AuthorWasFound)).ToList();

                    sb.AppendLine(string.Format("{0} / {1} ({2:P}) intersecting entries for A{3} and A{4}", result.Count, count, (double)result.Count / (double)count, i + 1, j + 1));
                }
            }

            File.WriteAllText(string.Format(basepath + "stats_intersect_pairwise{0}.txt", postfix), sb.ToString());
        }
    }
}
