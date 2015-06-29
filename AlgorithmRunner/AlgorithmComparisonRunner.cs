namespace ExpertiseExplorer.AlgorithmRunner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using ExpertiseExplorer.Algorithms;

    using ExpertiseDB;
    using ExpertiseDB.Extensions;
    using AlgorithmRunner.AbstractIssueTracker;
    using AlgorithmRunner.Bugzilla;

    using ExpertiseExplorer.Common;
    using ExpertiseExplorer.Algorithms.Statistics;
    using log4net;

    internal class AlgorithmComparisonRunner
    {
        private static readonly ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog OutputLog = LogManager.GetLogger("ExpertiseExplorer.AlgorithmRunner.Output");
        private static readonly ILog PerformanceLog = LogManager.GetLogger("ExpertiseExplorer.AlgorithmRunner.Performance");

        public int RepositoryId
        {
            get
            {
                return Algorithms[0].RepositoryId;
            }
        }

        public IList<AlgorithmBase> Algorithms { get; set; }

        public AliasFinder NameConsolidator { get; set; }

        public AlgorithmComparisonRunner(string sourceUrl, string basepath)
            : this(
                sourceUrl,
                basepath,
                new AlgorithmBase[]
                { 
                    new Line10RuleAlgorithm(),
                    new ExpertiseCloudAlgorithm(),
                    new DegreeOfAuthorshipAlgorithm(DegreeOfAuthorshipAlgorithm.WeightingType.UniversalTOSEM),
                    new ExperienceAtomsAlgorithm(),
                    new CodeOwnershipAlgorithm(),
                    new ExpertiseIntersectionAlgorithm()
                })
        {
        }

        public AlgorithmComparisonRunner(string sourceUrl, string basepath, IList<AlgorithmBase> algorithmsToEvaluate)
        {
            Algorithms = algorithmsToEvaluate;

            NameConsolidator = new AliasFinder();
            string authorMappingPath = basepath + "authors_consolidated.txt";
            if (File.Exists(authorMappingPath))
            {
                NameConsolidator.InitializeMappingFromAuthorList(File.ReadAllLines(authorMappingPath));
                foreach (AlgorithmBase algo in algorithmsToEvaluate)
                    algo.Deduplicator = NameConsolidator;
            }

            InitAlgorithms(sourceUrl);
        }

        protected void InitAlgorithms(string sourceUrl)
        {
            // Load Ids from DB for first algorithm
            Algorithms[0].InitIdsFromDbForSourceUrl(sourceUrl, false);

            foreach (AlgorithmBase algo in Algorithms.Skip(1))
            {
                algo.SourceRepositoryId = Algorithms[0].SourceRepositoryId;
                algo.RepositoryId = Algorithms[0].RepositoryId;
            }
        }

        public void StartComparisonFromFile(IssueTrackerEventFactory factory, DateTime resumeFrom, DateTime continueUntil, bool noComparison = false)
        {
            Log.Info("Starting comparison");
            IEnumerable<IssueTrackerEvent> list = factory.parseIssueTrackerEvents();
            HandleIssueTrackerEventList(list, resumeFrom, continueUntil, noComparison);
            Log.Info("Ending comparison");
        }

        #region Private Methods

        /// <summary>
        /// Go through a list of issue tracker activities. For each first patch upload of a bug, calculate expertises at the time of review and store five
        /// computed reviewers. For reviews, remember the actual reviewers for later comparison and grant experience for review-based algorithms.
        /// </summary>
        private void HandleIssueTrackerEventList(IEnumerable<IssueTrackerEvent> issueTrackerEventList, DateTime resumeFrom, DateTime continueUntil, bool noComparison)
        {
            DateTime timeAfterOneK = DateTime.Now;

            DateTime repositoryWatermark = DateTime.MinValue;   // Until which date are the number of deliveries, modifications, and so on counted?
            int count = 0;
            foreach (IssueTrackerEvent info in issueTrackerEventList)
            {
                ++count;
                if (count % 1000 == 0)
                {
                    Console.WriteLine("Now at: " + count);
                    PerformanceLog.Info(count + ";" + (DateTime.Now - timeAfterOneK).TotalMinutes);
                    timeAfterOneK = DateTime.Now;
                }

                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keypressed = Console.ReadKey(true);
                    switch (keypressed.Key)
                    {
                        case ConsoleKey.X:
                            Console.WriteLine("Now at: " + count);
                            Console.WriteLine("Time of next item (use with resume): " + info.When);
                            Console.WriteLine("Stopping due to user request.");

                            Log.Warn("Stopping due to user request. Time of next item (use with resume): " + info.When);
                            PerformanceLog.Info(count + ";" + (DateTime.Now - timeAfterOneK).TotalMinutes);
                            return;
                        case ConsoleKey.S:
                            Console.WriteLine("Now at: " + count);
                            break;
                        default:
                            Console.WriteLine("Press \"S\" for current status or \"X\" to initiate a stop of calculations.");
                            break;
                    }
                    while (Console.KeyAvailable)
                        Console.ReadKey(true);  // Flush input buffer
                }

                if (info.When > continueUntil)
                    return;
                if (info.When < resumeFrom)
                    continue;

                Log.Debug("Evaluating [" + info.GetType() + "]: " + info);

                const int NUMBER_OF_FAIL_RETRIES = 5;
                int retryNumber = 0;
                bool fSuccess = false;
                do
                {
                    try
                    {
                        ReviewInfo ri = info as ReviewInfo;
                        if (null != ri)
                            ProcessReviewInfo(ri, noComparison);

                        PatchUpload pu = info as PatchUpload;
                        if (null != pu && !noComparison)
                            ProcessPatchUpload(pu, noComparison, ref repositoryWatermark);

                        fSuccess = true;
                    }
                    catch (Exception ex)
                    {
                        if (++retryNumber <= NUMBER_OF_FAIL_RETRIES)
                        {        // try again, but wait a little, up to 50 * 5^3 = 6.25 seconds
                            Log.Error(ex);
                            System.Threading.Thread.Sleep(50 * retryNumber * retryNumber * retryNumber);
                        }
                        else
                        {
                            Log.Fatal("Error on handling an issue entry", ex);
                            Log.Info("The current entry \"" + info.ChangeId + "\" involves " + info.Filenames.Count + " files. Its type is [" + info.GetType() + "]. You should resume on " + info.When);
                            throw;
                        }
                    }
                }
                while (!fSuccess);

                OutputLog.Info(info);
            }
        }

        /// <summary>
        /// 1. Check whether this is the first upload for this bug (maybe not necessary)
        /// 2. Calculate algorithm values for the files in the bug
        /// 3. Calculate reviewers for the bug
        /// </summary>
        private void ProcessPatchUpload(IssueTrackerEvent info, bool noComparison, ref DateTime repositoryWatermark)
        {
            // 1. Check whether this is the first upload for this bug (maybe not necessary)

            using (ExpertiseDBEntities repository = new ExpertiseDBEntities())
            {
                Bug currentBug = repository.Bugs.SingleOrDefault(bug => bug.ChangeId == info.ChangeId);
                if (null != currentBug)
                    // Revisit: maybe the bug exists, but not the ComputedReviewers for the given algorithms.
                    return;
            }
            
            // 2. Calculate algorithm values for the files in the bug

            DateTime end = info.When;
            Algorithms[0].BuildConnectionsForSourceRepositoryBetween(repositoryWatermark, end);
            repositoryWatermark = end;

            IList<string> involvedFiles = info.Filenames;

            DateTime maxDateTime = Algorithms[0].MaxDateTime;
            int repositoryId = Algorithms[0].RepositoryId;
            int sourceRepositoryId = Algorithms[0].SourceRepositoryId;

            List<Task> tasks = new List<Task>();
            foreach (AlgorithmBase algorithm in Algorithms)
            {
                algorithm.MaxDateTime = maxDateTime;
                algorithm.RepositoryId = repositoryId;
                algorithm.SourceRepositoryId = sourceRepositoryId;

                tasks.Add(algorithm.CalculateExpertiseForFilesAsync(involvedFiles));
            }

            Task.WaitAll(tasks.ToArray());

            if (noComparison)
                return;

            // 3. Calculate reviewers for the bug

            IEnumerable<int> artifactIds = involvedFiles
                .Select(fileName => Algorithms[0].FindOrCreateFileArtifactId(fileName));

            // Create a list of tasks, one for each algorithm, that compute reviewers for the artifact
            IEnumerable<Task<ComputedReviewer>> computedReviewerTasks = Algorithms.Select(algorithm => algorithm.GetDevelopersForArtifactsAsync(artifactIds)).ToList();

            Task.WaitAll(computedReviewerTasks.ToArray());

            using (ExpertiseDBEntities repository = new ExpertiseDBEntities())
            {
                Bug currentBug = new Bug()
                {
                    ChangeId = info.ChangeId,
                    RepositoryId = this.RepositoryId
                };
                repository.Bugs.Add(currentBug);

                foreach (Task<ComputedReviewer> task in computedReviewerTasks)
                {
                    currentBug.ComputedReviewers.Add(task.Result);
                }

                repository.SaveChanges();
            }
        }

        /// <summary>
        /// Handles a review by doing two things:
        ///  - Store in DB that the reviewer is a possible reviewer in this bug/change.
        ///  - Grant the reviewer review experience for the review.
        /// </summary>
        protected void ProcessReviewInfo(ReviewInfo info, bool noComparison)
        {
            // Store in DB that the reviewer is a possible reviewer in this bug/change.
            if (!noComparison)
                using (ExpertiseDBEntities repository = new ExpertiseDBEntities())
                {
                    Bug theBug = repository.Bugs.Single(bug => bug.ChangeId == info.ChangeId);

                    bool fBugDirty = false;
                    foreach (string primaryName in NameConsolidator.DeanonymizeAuthor(info.Reviewer))   // this is usually just one
                    {
                        if (theBug.ActualReviewers.Any(reviewer => reviewer.Reviewer == primaryName))
                            continue;     //  the reviewer is already in the list

                        theBug.ActualReviewers.Add(new ActualReviewer()
                            {
                                ActivityId = info.ActivityId,
                                Bug = theBug,
                                Reviewer = primaryName
                            });
                        fBugDirty = true;
                    }

                    if (fBugDirty)
                        repository.SaveChanges();
                }

            // Grant the reviewer review experience for the review
            foreach (ReviewAlgorithmBase reviewAlgorithm in Algorithms.OfType<ReviewAlgorithmBase>())
                reviewAlgorithm.AddReviewScore(info.Reviewer, info.Filenames, info.When);
         }
        #endregion
    }
}
