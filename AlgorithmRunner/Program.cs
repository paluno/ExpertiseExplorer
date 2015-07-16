namespace ExpertiseExplorer.AlgorithmRunner
{
    using ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker;
    using ExpertiseExplorer.AlgorithmRunner.Bugzilla;
    using ExpertiseExplorer.AlgorithmRunner.Gerrit;
    using ExpertiseExplorer.Algorithms;
    using ExpertiseExplorer.Algorithms.FPS;
    using System;

    using ExpertiseExplorer.Common;
    using System.Collections.Generic;

    public class Program
    {
        public enum ReviewSourceType { Bugzilla, Gerrit };

        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            if (args.Length < 4)
            {
                ShowHelp();
                return;
            }

            string sourceUrlIdentifier = args[0];
            ReviewSourceType reviewSourceType = (ReviewSourceType)Enum.Parse(typeof(ReviewSourceType), args[1], true);
            string basepath = args[2];
            string mode = args[3].ToLower();

            switch (mode)
            {
                case "c":
                case "clean":
                    var cleaner = new FilenameCleanup();
                    cleaner.StartCleanup(basepath + @"CrawlerOutput\combinedoutput_reduced.txt", basepath + @"CrawlerOutput\combinedoutput_reduced_clean.txt");
                    return;

                case "a":
                case "algorithm":
                    var forceOverwrite = false;
                    var noComp = false;
                    DateTime? resumeTime = DateTime.MinValue;
                    DateTime maxTime = DateTime.MaxValue;
                    string algoSelectString = "1cdaoif";    // the default: All algorithms

                    if (args.Length > 4)
                    {
                        for (int i = 4; i < args.Length; i++)
                        {
                            if (args[i].StartsWith("algo-"))
                            {
                                algoSelectString = args[i].Substring("algo-".Length);
                                continue;
                            }

                            switch (args[i].ToLower())
                            {
                                case "f":
                                case "force":
                                    forceOverwrite = true;
                                    break;

                                case "n":
                                case "nocomp":
                                    noComp = true;
                                    break;

                                case "r":
                                case "resume":

                                    if (++i == args.Length)
                                    {
                                        Console.WriteLine("Error: resume argument is missing the parameter.");
                                        return;
                                    }

                                    if (string.Equals(args[i], "AUTO", StringComparison.OrdinalIgnoreCase))
                                        resumeTime = null;
                                    else
                                    {
                                        DateTime utcTimeOfLastComparison;
                                        long unixTimeOfLastComparison;
                                        if (long.TryParse(args[i], out unixTimeOfLastComparison))
                                            resumeTime = unixTimeOfLastComparison.UnixTime2UTCDateTime() - new TimeSpan(0, 0, 0, 1);
                                        else if (DateTime.TryParse(args[i], out utcTimeOfLastComparison))
                                            resumeTime = utcTimeOfLastComparison;
                                        else
                                        {
                                            Console.WriteLine("Error: Unable to parse {0} as int or DateTime.", args[i]);
                                            return;
                                        }
                                        resumeTime = resumeTime.Value.ToUniversalTime();
                                    }

                                    break;
                                case "m":
                                case "max":
                                    if (++i >= args.Length)
                                    {
                                        Console.WriteLine("Error: max argument is missing the parameter.");
                                        return;
                                    }

                                    long timeOfMaxComparison;
                                    if (long.TryParse(args[i], out timeOfMaxComparison))
                                        maxTime = timeOfMaxComparison.UnixTime2UTCDateTime();
                                    else if (!DateTime.TryParse(args[i], out maxTime))
                                    {
                                        Console.WriteLine("Error: Unable to parse {0} as int.", args[i]);
                                        return;
                                    }

                                    break;
                                default:
                                    Console.WriteLine("Error: Unknown argument {0}.", args[i]);
                                    break;
                            }
                        }

                    }

                    IssueTrackerEventFactory factory;
                    switch (reviewSourceType)
                    {
                        case ReviewSourceType.Bugzilla:
                            BugzillaAttachmentFactory baf = new BugzillaAttachmentFactory(basepath + @"attachments_final.txt");
                            baf.PrepareInput(basepath + @"attachments.txt", forceOverwrite);
                            factory = new BugzillaReviewFactory(basepath + "input_final.txt", baf);
                            if (forceOverwrite)
                                ((BugzillaReviewFactory)factory).filterForUsedAttachmentsAndPersist();
                            break;
                        case ReviewSourceType.Gerrit:
                            factory = new GerritReviewFactory(basepath + "input_final.csv");
                            break;
                        default:
                            Console.WriteLine("Error: Unknown type of review data: {0}", reviewSourceType);
                            ShowHelp();
                            return;
                    }

                    factory.PrepareInput(basepath + "input.txt", forceOverwrite);

                    AlgorithmComparisonRunner comparisonRunner = new AlgorithmComparisonRunner(sourceUrlIdentifier, basepath, selectAlgorithmsFromString(algoSelectString));
                    comparisonRunner.StartComparisonFromFile(factory, resumeTime, maxTime, noComp);

                    return;

                default:
                    ShowHelp();
                    break;
            }
        }

        private static System.Collections.Generic.IList<AlgorithmBase> selectAlgorithmsFromString(string algoSelectString)
        {
            string algoSelectUpperCase = algoSelectString.ToUpperInvariant();
            List<AlgorithmBase> algorithms = new List<AlgorithmBase>();

            if (algoSelectUpperCase.Contains("1"))
                algorithms.Add(new Line10RuleAlgorithm());
            if (algoSelectUpperCase.Contains("C"))
                algorithms.Add(new ExpertiseCloudAlgorithm());
            if (algoSelectUpperCase.Contains("D"))
                algorithms.Add(new DegreeOfAuthorshipAlgorithm(DegreeOfAuthorshipAlgorithm.WeightingType.UniversalTOSEM));
            if (algoSelectUpperCase.Contains("A"))
                algorithms.Add(new ExperienceAtomsAlgorithm());
            if (algoSelectUpperCase.Contains("O"))
                algorithms.Add(new CodeOwnershipAlgorithm());
            if (algoSelectUpperCase.Contains("I"))
                algorithms.Add(new ExpertiseIntersectionAlgorithm());
            if (algoSelectUpperCase.Contains("F"))
            {
                RootDirectory fpsTree = new RootDirectory();
                WeighedReviewCountAlgorithm wrcAlgo = new WeighedReviewCountAlgorithm(fpsTree);
                wrcAlgo.LoadReviewScoresFromDB();
                algorithms.Add(wrcAlgo);
                algorithms.Add(new FPSReviewAlgorithm(fpsTree));
            }

            return algorithms;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("AlgorithmRunner.exe name type path mode [arg0....argN]");
            Console.WriteLine("name: the sourceRepository.sourceUrl identifier");
            Console.WriteLine("type: the type of review data to read: BUGZILLA or GERRIT");
            Console.WriteLine("path: the path from which to read the crawler output and where all generated output will be saved");
            Console.WriteLine("mode: the operating mode, can be:");
            Console.WriteLine("\t c or clean for filename cleanup of crawled filenames (filters everything that does not belong to the Firefox project e.g. files from camino, wallet)");
            Console.WriteLine("\t additional argument: none\n");
            Console.WriteLine("\t a or algorithm for algorithm comparison");
            Console.WriteLine("\t additional argument: algo-1cdaoif to restrict the comparison to some algorithms. Each letter stand for one algorithm that will be evaluated if present:");
            Console.WriteLine("\t\t 1 - Line 10 Rule");
            Console.WriteLine("\t\t c - Expertise Cloud");
            Console.WriteLine("\t\t d - Degree-Of-Authorship (TOSEM style)");
            Console.WriteLine("\t\t a - Experience Atoms");
            Console.WriteLine("\t\t o - Code Ownership");
            Console.WriteLine("\t\t i - Expertise Intersection");
            Console.WriteLine("\t\t f - File-Path-Similarity (Review-based algorithm)");
            Console.WriteLine("\t additional argument: f or force for forcing an existing prepared input to be overwritten (optional)");
            Console.WriteLine("\t additional argument: n or nocomp for only creating expertise values from revision, skipping the comparison (optional)");
            Console.WriteLine("\t additional argument: r or resume arg for resuming the computation from arg datetime, arg can be in unix time (optional)");
            Console.WriteLine("\t \t Useful for resuming the calculations after an error. Use repository.lastUpdate as the value for arg\n");
            Console.WriteLine("\t additional argument: m or max to compute expertises only up to arg datetime, arg has to be in unix time (optional)");
            Console.WriteLine("\t Example of usage:");
            Console.WriteLine("\t AlgorithmRunner.exe Firefox Bugzilla C:\\ExpertiseExplorerOutput\\ a f r 1353412846\n");
        }
    }
}
