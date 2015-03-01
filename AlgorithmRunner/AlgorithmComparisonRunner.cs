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
        private const string FLAGS = "flags";

        private const string REVIEWPLUS = "review+";

        private const string REVIEWMINUS = "review-";

        private const string SUPERREVIEWREQUEST = "superreview?";
        
        private const string SUPERREVIEWPLUS = "superreview+";

        private const string SUPERREVIEWMINUS = "superreview-";

        private const string ATTACHMENTIDENTIFIER = "attachment #";

        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<int, List<string>> _attachments = new Dictionary<int, List<string>>();

        private readonly string _foundFilesOnlyPath;

        private readonly string _performancePath;

        public AlgorithmComparisonRunner(string sourceUrl, string basepath)
        {
            _foundFilesOnlyPath = basepath + "output.txt";
            _performancePath = basepath + "performance_log.txt";

            var attachmentFilePath = basepath + @"CrawlerOutput\attachments.txt";
            var attachmentLines = File.ReadAllLines(attachmentFilePath);
            foreach (var attachmentLine in attachmentLines)
            {
                var attachmentId = int.Parse(attachmentLine.Split(';')[1]);
                _attachments.Add(attachmentId, attachmentLine.Split(';')[2].Split(',').Distinct().ToList());
            }

            Algorithms = new List<AlgorithmBase>
            { 
                new Line10RuleAlgorithm(),
                new ExpertiseCloudAlgorithm(),
                new DegreeOfAuthorshipAlgorithm(),
                new ExperienceAtomsAlgorithm(),
                new CodeOwnershipAlgorithm(),
                new ExpertiseIntersectionAlgorithm()
            };

            // Load Ids from DB for first algorithm, gets set for all other later
            Algorithms[0].InitIdsFromDbForSourceUrl(sourceUrl, false);
        }

        public List<AlgorithmBase> Algorithms { get; set; }

        public void StartComparisonFromFile(string filename, DateTime resumeFrom, bool noComparison = false)
        {
            var starttime = DateTime.Now;
            Debug.WriteLine("Starting comparison at: " + starttime);
            var list = GetActivityInfoFromFile(filename);
            HandleActivityInfoList(list, resumeFrom, noComparison);
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
                filteredInput = ParseAndFilterInput(input);
            }
            finally
            {
                input.Close();
            }

            Debug.WriteLine("Finished parsing at: " + DateTime.Now);

            File.WriteAllText(pathToOutputFile, filteredInput);

            Debug.WriteLine("Starting ordering at: " + DateTime.Now);

            var list = GetActivityInfoFromFile(pathToOutputFile);
            
            // ordering of & another filter pass on the activities
            var mercurialTransferDate = new DateTime(2007, 3, 22, 18, 29, 0); // date of Mozilla's move to hg
            var endOfHgDump = new DateTime(2013, 3, 8, 16, 15, 44); // last date of the hg dump
            var timeOrder = new List<long>();
            var activityLookupTable = new Dictionary<long, List<ActivityInfo>>();
            foreach (var activityInfo in list)
            {
                // filter if not review
                if (!IsReview(activityInfo))
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

        private static ComputedReviewer GetDevelopersForArtifactAndAlgorithm(int artifactId, int algorithmId)
        {
            using (var entities = new ExpertiseDBEntities())
            {
                var developers = entities.GetDevelopersForArtifactAndAlgorithm(artifactId, algorithmId).OrderByDescending(sde => sde.Expertise).Take(5).ToList();
                while (developers.Count < 5)
                    developers.Add(new SimplifiedDeveloperExpertise { DeveloperId = 0, DeveloperName = string.Empty, Expertise = 0d });

                return new ComputedReviewer
                {
                    Expert1 = developers[0].DeveloperName,
                    Expert1Value = developers[0].Expertise,
                    Expert2 = developers[1].DeveloperName,
                    Expert2Value = developers[1].Expertise,
                    Expert3 = developers[2].DeveloperName,
                    Expert3Value = developers[2].Expertise,
                    Expert4 = developers[3].DeveloperName,
                    Expert4Value = developers[3].Expertise,
                    Expert5 = developers[4].DeveloperName,
                    Expert5Value = developers[4].Expertise,
                    AlgorithmId = algorithmId
                };
            }
        }
        
        private static bool IsReview(ActivityInfo activityInfo)
        {
            return activityInfo.Added.Contains(REVIEWPLUS) || activityInfo.Added.Contains(REVIEWMINUS);
        }

        // removes supperreview strings and afterwards returns only lines that stll contain 'review+' or 'review-'
        private static string ParseAndFilterInput(TextReader input)
        {
            var result = new StringBuilder();
            string line;
            while ((line = input.ReadLine()) != null)
            {
                line = line.ToLower();
                line = line.Replace(SUPERREVIEWREQUEST, string.Empty);
                line = line.Replace(SUPERREVIEWPLUS, string.Empty);
                line = line.Replace(SUPERREVIEWMINUS, string.Empty);
                if ((line.Contains(REVIEWMINUS) || line.Contains(REVIEWPLUS)) && line.Contains(FLAGS))
                    result.AppendLine(line);
            }

            return result.ToString();
        }

        private static IEnumerable<ActivityInfo> GetActivityInfoFromFile(string pathToInputFile)
        {
            var input = new StreamReader(pathToInputFile);
            var result = new List<ActivityInfo>();
            Debug.WriteLine("Starting ActivityInfo parsing at: " + DateTime.Now);
            try
            {
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    line = line.ToLower();
                    var fields = line.Split(';');
                    var activityInfo = new ActivityInfo
                    {
                        BugId = int.Parse(fields[0]),
                        ActivityId = int.Parse(fields[1]),
                        Author = fields[2],
                        What = fields[4],
                        Removed = fields[5],
                        Added = fields[6]
                    };

                    activityInfo.SetDateTimeFromUnixTime(long.Parse(fields[3]));
                    result.Add(activityInfo);
                }
            }
            finally
            {
                input.Close();
            }

            Debug.WriteLine("Finished ActivityInfo parsing at: " + DateTime.Now);

            return result;
        }

        private void HandleActivityInfoList(IEnumerable<ActivityInfo> activityInfo, DateTime resumeFrom, bool noComparison)
        {
            var found = new StreamWriter(_foundFilesOnlyPath, true);
            var performanceLog = new StreamWriter(_performancePath, true);
            var timeAfterOneK = DateTime.Now;

            var start = DateTime.MinValue;
            var count = 0;
            var stopwatch = new Stopwatch();
            foreach (var info in activityInfo)
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

                if (info.When < resumeFrom)
                    continue;

                var involvedFiles = GetFilesFromActivityInfo(info);

                var end = info.When;
                Algorithms[0].BuildConnectionsForSourceRepositoryBetween(start, end);
                start = end;

                var maxDateTime = Algorithms[0].MaxDateTime;
                var repositoryId = Algorithms[0].RepositoryId;
                var sourceRepositoryId = Algorithms[0].SourceRepositoryId;
                
                try
                {
                    var tasks = new List<Task>();
                    foreach (var algorithm in Algorithms)
                    {
                        algorithm.MaxDateTime = maxDateTime;
                        algorithm.RepositoryId = repositoryId;
                        algorithm.SourceRepositoryId = sourceRepositoryId;

                        var fixMyClosure = algorithm;
                        tasks.Add(Task.Factory.StartNew(
                            input => fixMyClosure.CalculateExpertiseForFiles(input as List<string>),
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

                if (noComparison)
                    continue;

                foreach (var involvedFile in involvedFiles)
                {
                    var artifactId = Algorithms[0].GetArtifactIdFromArtifactnameApproximation(involvedFile);
                    if (artifactId < 0)
                        throw new FileNotFoundException(string.Format("Artifact {0} not found", involvedFile));

                    var actualReviewer = new ActualReviewer
                    {
                        ActivityId = info.ActivityId,
                        ArtifactId = artifactId,
                        BugId = info.BugId,
                        Reviewer = info.Author,
                        Time = info.When
                    };

                    var tasks = Algorithms.Select(algorithm => Task<ComputedReviewer>.Factory.StartNew(() => GetDevelopersForArtifactAndAlgorithm(artifactId, algorithm.AlgorithmId))).ToList();

                    Task.WaitAll(tasks.ToArray());
                    foreach (var task in tasks)
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

            found.Close();
            performanceLog.Close();
        }

        private List<string> GetFilesFromActivityInfo(ActivityInfo activityInfo)
        {
            if (activityInfo.What.Contains(ATTACHMENTIDENTIFIER))
            {
                var attachmentIdString = activityInfo.What.Replace(ATTACHMENTIDENTIFIER, string.Empty);
                attachmentIdString = attachmentIdString.Replace(FLAGS, string.Empty);
                attachmentIdString = attachmentIdString.Trim();
                var attachmentId = int.Parse(attachmentIdString);

                return _attachments[attachmentId];
            }

            return new List<string>();
        }

        #endregion
    }
}
