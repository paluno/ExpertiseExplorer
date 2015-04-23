namespace AlgorithmRunner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Algorithms;

    using ExpertiseDB;
    using ExpertiseDB.Extensions;

    internal class AlgorithmComparisonRunner
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _foundFilesOnlyPath;

        private readonly string _performancePath;

        public IList<AlgorithmBase> Algorithms { get; set; }

        public AlgorithmComparisonRunner(string sourceUrl, string basepath)
            : this(
                sourceUrl,
                basepath,
                new AlgorithmBase[]
                { 
                    new Line10RuleAlgorithm(),
                    new ExpertiseCloudAlgorithm(),
                    new DegreeOfAuthorshipAlgorithm(),
                    new ExperienceAtomsAlgorithm(),
                    new CodeOwnershipAlgorithm(),
                    new ExpertiseIntersectionAlgorithm()
                })
        {
        }

        public AlgorithmComparisonRunner(string sourceUrl, string basepath, IList<AlgorithmBase> algorithmsToEvaluate)
        {
            Algorithms = algorithmsToEvaluate;

            _foundFilesOnlyPath = basepath + "output.txt";
            _performancePath = basepath + "performance_log.txt";

            //var attachmentFilePath = basepath + @"CrawlerOutput\attachments.txt";

            InitAlgorithms(sourceUrl);
        }

        protected void InitAlgorithms(string sourceUrl)
        {
            // Load Ids from DB for first algorithm, gets set for all other later
            Algorithms[0].InitIdsFromDbForSourceUrl(sourceUrl, false);
        }

        public void StartComparisonFromFile(ReviewInfoFactory factory, DateTime resumeFrom, DateTime continueUntil, bool noComparison = false)
        {
            DateTime starttime = DateTime.Now;
            Debug.WriteLine("Starting comparison at: " + starttime);
            IEnumerable<ReviewInfo> list = factory.parseReviewInfos();
            HandleReviewInfoList(list, resumeFrom, continueUntil, noComparison);
            Debug.WriteLine("Ending comparison at: " + DateTime.Now);
            Debug.WriteLine("Time: " + (DateTime.Now - starttime));
        }

        #region Private Methods

        /// <summary>
        /// Go through a list of review activities. For each review, calculate expertises at the time of review and store five
        /// computed reviewers
        /// </summary>
        private void HandleReviewInfoList(IEnumerable<ReviewInfo> reviewInfo, DateTime resumeFrom, DateTime continueUntil, bool noComparison)
        {
            using (StreamWriter found = new StreamWriter(_foundFilesOnlyPath, true))
            using (StreamWriter performanceLog = new StreamWriter(_performancePath, true))
            {
                DateTime timeAfterOneK = DateTime.Now;

                DateTime start = DateTime.MinValue;
                int count = 0;
                Stopwatch stopwatch = new Stopwatch();
                foreach (ReviewInfo info in reviewInfo)
                {
                    stopwatch.Start();
                    count++;
                    if (count % 1000 == 0)
                    {
                        Console.WriteLine("Now at: " + count);
                        performanceLog.WriteLine(count + ";" + (DateTime.Now - timeAfterOneK).TotalMinutes);
                        timeAfterOneK = DateTime.Now;
                        performanceLog.Flush();
                    }

                    if (Console.KeyAvailable)
                    {
                        ConsoleKeyInfo keypressed = Console.ReadKey(true);
                        switch (keypressed.Key)
                        {
                            case ConsoleKey.X:
                                Console.WriteLine("Now at: " + count);
                                Console.WriteLine("Time of next item (use with resume): " + ActivityInfo.PDTDateTime2unixTime(info.When));
                                Console.WriteLine("Stopping due to user request.");

                                Log.Info("Stopping due to user request. Time of next item (use with resume): " + ActivityInfo.PDTDateTime2unixTime(info.When));
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

                    IList<string> involvedFiles = info.Filenames;

                    DateTime end = info.When;
                    Algorithms[0].BuildConnectionsForSourceRepositoryBetween(start, end);
                    start = end;

                    ProcessReviewInfo(info, involvedFiles, found, stopwatch);

                    if (noComparison)
                        continue;

                    foreach (var involvedFile in involvedFiles)
                    {
                        int artifactId = Algorithms[0].FindOrCreateFileArtifactIdFromArtifactnameApproximation(involvedFile);

                        ActualReviewer actualReviewer = new ActualReviewer
                        {
                            ActivityId = info.ActivityId,
                            ArtifactId = artifactId,
                            ChangeId = info.ChangeId,
                            Reviewer = info.Reviewer,
                            Time = info.When
                        };

                        // Create a list of tasks, one for each algorithm, that compute reviewers for the artifact
                        IEnumerable<Task<ComputedReviewer>> tasks = Algorithms.Select(algorithm => Task<ComputedReviewer>.Factory.StartNew(() => algorithm.GetDevelopersForArtifact(artifactId))).ToList();

                        Task.WaitAll(tasks.ToArray());
                        foreach (Task<ComputedReviewer> task in tasks)
                        {
                            actualReviewer.ComputedReviewers.Add(task.Result);
                        }

                        using (var entities = new ExpertiseDBEntities())
                        {
                            entities.ActualReviewers.Add(actualReviewer);
                            entities.SaveChanges();
                        }
                    }

                    stopwatch.Stop();
                    Log.Info("-- " + stopwatch.Elapsed);
                }
            }
        }

        protected virtual void ProcessReviewInfo(ReviewInfo info, IList<string> involvedFiles, StreamWriter found, Stopwatch stopwatch)
        {
            DateTime maxDateTime = Algorithms[0].MaxDateTime;
            int repositoryId = Algorithms[0].RepositoryId;
            int sourceRepositoryId = Algorithms[0].SourceRepositoryId;

            const int NUMBER_OF_FAIL_RETRIES = 5;
            int retryNumber = 0;
            bool fSuccess = false;
            do
            {
                try
                {
                    List<Task> tasks = new List<Task>();
                    foreach (AlgorithmBase algorithm in Algorithms)
                    {
                        algorithm.MaxDateTime = maxDateTime;
                        algorithm.RepositoryId = repositoryId;
                        algorithm.SourceRepositoryId = sourceRepositoryId;

                        AlgorithmBase fixMyClosure = algorithm; // Maybe fixMyClosure is necessary as otherwise all tasks would use the last algorithm?
                        tasks.Add(Task.Factory.StartNew(
                            input => fixMyClosure.CalculateExpertiseForFiles(input as IList<string>),
                            involvedFiles));
                    }

                    Task.WaitAll(tasks.ToArray());
                    stopwatch.Stop();
                    Log.Info("- " + stopwatch.Elapsed);
                    stopwatch.Start();

                    found.WriteLine(info.ToString());
                    found.Flush();
                    fSuccess = true;
                }
                catch (AggregateException ae)
                {
                    if (++retryNumber <= NUMBER_OF_FAIL_RETRIES)
                    {        // try again, but wait a little, up to 50 * 5^3 = 6.25 seconds
                        Log.Error(ae);
                        System.Threading.Thread.Sleep(50 * retryNumber * retryNumber * retryNumber);
                    }
                    else
                    {
                        Log.Fatal(ae);
                        Log.Info("The current job involves " + involvedFiles.Count + " files. You should resume with " + ActivityInfo.PDTDateTime2unixTime(info.When));
                        throw;
                    }

                    //foreach (var ex in ae.Flatten().InnerExceptions)
                    //{
                    //    if (ex is FileNotFoundException)
                    //        Log.Error(ex.Message);
                    //    else
                    //        throw;
                    //}
                }
            }
            while (!fSuccess);

            return;
        }



        #endregion
    }
}
