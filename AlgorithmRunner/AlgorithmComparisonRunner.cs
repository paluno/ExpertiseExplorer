﻿namespace AlgorithmRunner
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

        private readonly Dictionary<int, List<string>> _attachments = new Dictionary<int, List<string>>();

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

            var attachmentFilePath = basepath + @"CrawlerOutput\attachments.txt";
            var attachmentLines = File.ReadAllLines(attachmentFilePath);
            foreach (var attachmentLine in attachmentLines)
            {
                var attachmentId = int.Parse(attachmentLine.Split(';')[1]);
                _attachments.Add(attachmentId, attachmentLine.Split(';')[2].Split(',').Distinct().ToList());
            }

            InitAlgorithms(sourceUrl);  
        }

        protected void InitAlgorithms(string sourceUrl)
        {
            // Load Ids from DB for first algorithm, gets set for all other later
            Algorithms[0].InitIdsFromDbForSourceUrl(sourceUrl, false);
        }

        public void StartComparisonFromFile(string filename, DateTime resumeFrom, DateTime continueUntil, bool noComparison = false)
        {
            DateTime starttime = DateTime.Now;
            Debug.WriteLine("Starting comparison at: " + starttime);
            IEnumerable<ActivityInfo> list = ActivityInfo.GetActivityInfoFromFile(filename);
            HandleActivityInfoList(list, resumeFrom, continueUntil, noComparison);
            Debug.WriteLine("Ending comparison at: " + DateTime.Now);
            Debug.WriteLine("Time: " + (DateTime.Now - starttime));
        }

        // parses, filters and orders the bugzilla activity log
        public void PrepareInput(string pathToInputFile, string pathToOutputFile, bool overwrite = false)
        {
            if (!overwrite && File.Exists(pathToOutputFile))
                return;

            var input = new StreamReader(pathToInputFile);
            string filteredInput;
            Debug.WriteLine("Starting parsing at: " + DateTime.Now);
            try
            {
                filteredInput = ActivityInfo.ParseAndFilterInput(input);
            }
            finally
            {
                input.Close();
            }

            Debug.WriteLine("Finished parsing at: " + DateTime.Now);

            File.WriteAllText(pathToOutputFile, filteredInput);

            Debug.WriteLine("Starting ordering at: " + DateTime.Now);

            var list = ActivityInfo.GetActivityInfoFromFile(pathToOutputFile);
            
            // ordering of & another filter pass on the activities
            var mercurialTransferDate = new DateTime(2007, 3, 22, 18, 29, 0); // date of Mozilla's move to hg
            var endOfHgDump = new DateTime(2013, 3, 8, 16, 15, 44); // last date of the hg dump
            var timeOrder = new List<long>();
            var activityLookupTable = new Dictionary<long, List<ActivityInfo>>();
            foreach (var activityInfo in list)
            {
                // filter if not review
                if (!activityInfo.IsReview)
                    continue;

                // filter if not in examined window of time
                if (activityInfo.When < mercurialTransferDate || activityInfo.When > endOfHgDump)
                    continue;

                var involvedFiles = GetFilesFromActivityInfo(activityInfo);

                // filter if there are no files
                if (involvedFiles.Count == 0)
                    continue;

                // filter if there is only one file with no name 
                if (involvedFiles.Count == 1 && involvedFiles[0] == string.Empty)
                    continue;

                var key = activityInfo.UnixTime;
                timeOrder.Add(key);

                if (!activityLookupTable.ContainsKey(key))
                    activityLookupTable.Add(key, new List<ActivityInfo>());

                activityLookupTable[key].Add(activityInfo);
            }

            // needed b/c bugzilla logs are ordered according to bugid, not datetime
            timeOrder = timeOrder.Distinct().ToList();
            timeOrder.Sort();
            var sb = new StringBuilder();
            foreach (var activityInfo in timeOrder.SelectMany(unixTime => activityLookupTable[unixTime]))
            {
                sb.AppendLine(activityInfo.ToString());
            }

            Debug.WriteLine("Finished ordering at: " + DateTime.Now);

            File.WriteAllText(pathToOutputFile, sb.ToString());
        }

        #region Private Methods
        
        /// <summary>
        /// Go through a list of review activities. For each review, calculate expertises at the time of review and store five
        /// computed reviewers
        /// </summary>
        private void HandleActivityInfoList(IEnumerable<ActivityInfo> activityInfo, DateTime resumeFrom, DateTime continueUntil, bool noComparison)
        {
            using (StreamWriter found = new StreamWriter(_foundFilesOnlyPath, true))
            using (StreamWriter performanceLog = new StreamWriter(_performancePath, true))
            {
                DateTime timeAfterOneK = DateTime.Now;

                DateTime start = DateTime.MinValue;
                int count = 0;
                Stopwatch stopwatch = new Stopwatch();
                foreach (ActivityInfo info in activityInfo)
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
                        switch(keypressed.Key)
                        {
                            case ConsoleKey.X:
                                Console.WriteLine("Now at: " + count);
                                Console.WriteLine("Time of next item (use with resume): " + ActivityInfo.PDTDateTime2unixTime(info.When));
                                Console.WriteLine("Stopping due to user request.");

                                Log.Info("Stopping due to user request.");
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

                    IList<string> involvedFiles = GetFilesFromActivityInfo(info);

                    DateTime end = info.When;
                    Algorithms[0].BuildConnectionsForSourceRepositoryBetween(start, end);
                    start = end;

                    ProcessActivityInfo(info, involvedFiles, found, stopwatch);

                    if (noComparison)
                        continue;

                    foreach (var involvedFile in involvedFiles)
                    {
                        int artifactId = Algorithms[0].GetArtifactIdFromArtifactnameApproximation(involvedFile);
                        if (artifactId < 0)
                            throw new FileNotFoundException(string.Format("Artifact {0} not found", involvedFile));

                        ActualReviewer actualReviewer = new ActualReviewer
                        {
                            ActivityId = info.ActivityId,
                            ArtifactId = artifactId,
                            BugId = info.BugId,
                            Reviewer = info.Author,
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

        protected virtual void ProcessActivityInfo(ActivityInfo info, IList<string> involvedFiles, StreamWriter found, Stopwatch stopwatch)
        {
            DateTime maxDateTime = Algorithms[0].MaxDateTime;
            int repositoryId = Algorithms[0].RepositoryId;
            int sourceRepositoryId = Algorithms[0].SourceRepositoryId;

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
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.Flatten().InnerExceptions)
                {
                    if (ex is FileNotFoundException)
                        Log.Error(ex.Message);
                    else
                        throw;
                }
            }

            return;
        }

        /// <summary>
        /// Get a list of all file names that are involved in a activity
        /// </summary>
        private IList<string> GetFilesFromActivityInfo(ActivityInfo activityInfo)
        {
            int? attachmentId = activityInfo.GetAttachmentId();
            if (attachmentId.HasValue)
                return _attachments[attachmentId.Value];
            else
                return new List<string>();
        }

        #endregion
    }
}
