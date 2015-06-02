namespace AlgorithmRunner
{
    using AlgorithmRunner.AbstractIssueTracker;
    using AlgorithmRunner.Bugzilla;
    using AlgorithmRunner.Gerrit;
    using Algorithms;
    using System;

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
                case "review":
                case "t":
                case "tosem":
                    var forceOverwrite = false;
                    var noComp = false;
                    int timeOfLastComparison = 0;
                    int timeOfMaxComparison = int.MaxValue;
                    DateTime resumeTime = DateTime.MinValue;
                    DateTime maxTime = DateTime.MaxValue;

                    AlgorithmComparisonRunner comparisonRunner;
                    if ("review" == mode)
                    {
                        comparisonRunner = new ReviewerAlgorithmComparisonRunner(sourceUrlIdentifier, basepath);
                        ((ReviewerAlgorithmComparisonRunner)comparisonRunner).InitFromDB();
                    }
                    else if ("t" == mode || "tosem" == mode)
                    {
                        comparisonRunner = new AlgorithmComparisonRunner(sourceUrlIdentifier, basepath, new AlgorithmBase[] { 
                            new DegreeOfAuthorshipAlgorithm(DegreeOfAuthorshipAlgorithm.WeightingType.UniversalTOSEM)
                        });
                    }
                    else if ("a" == mode || "algorithm" == mode)
                        comparisonRunner = new AlgorithmComparisonRunner(sourceUrlIdentifier, basepath);
                    else
                    {
                        Console.WriteLine("Invalid mode \"" + mode + "\""); // cannot happen if the switch is okay
                        return;
                    }

                    if (args.Length > 4)
                    {
                        for (int i = 4; i < args.Length; i++)
                        {
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

                                    if (!int.TryParse(args[i], out timeOfLastComparison))
                                    {
                                        Console.WriteLine("Error: Unable to parse {0} as int.", args[i]);
                                        return;
                                    }

                                    break;
                                case "m":
                                case "max":
                                    if (++i >= args.Length)
                                    {
                                        Console.WriteLine("Error: max argument is missing the parameter.");
                                        return;
                                    }

                                    if (!int.TryParse(args[i], out timeOfMaxComparison))
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

                        resumeTime = BugzillaReview.UnixTime2PDTDateTime(timeOfLastComparison) - new TimeSpan(0, 0, 0, 1);
                        maxTime = BugzillaReview.UnixTime2PDTDateTime(timeOfMaxComparison);
                    }

                    IssueTrackerEventFactory factory;

                    switch (reviewSourceType)
                    {
                        case ReviewSourceType.Bugzilla:
                            factory = new BugzillaReviewFactory(basepath + "input_final.txt", basepath + @"CrawlerOutput\attachments.txt");
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
                    comparisonRunner.StartComparisonFromFile(factory, resumeTime, maxTime, noComp);

                    return;

                default:
                    ShowHelp();
                    break;
            }
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
            Console.WriteLine("\t review is also algorithm comparision, but using the review algorithm set instead of the usual algorithms");
            Console.WriteLine("\t t or tosem is again an algorithm comparision. It only uses the degree-of-knowledge algorithm with the weightings from the ACM TOSEM paper.");
            Console.WriteLine("\t additional argument: f or force for forcing an existing prepared input to be overwritten (optional)");
            Console.WriteLine("\t additional argument: n or nocomp for only creating expertise values from revision, skipping the comparison (optional)");
            Console.WriteLine("\t additional argument: r or resume arg for resuming the computation from arg datetime, arg has to be in unix time (optional)");
            Console.WriteLine("\t \t Useful for resuming the calculations after an error. Use repository.lastUpdate as the value for arg\n");
            Console.WriteLine("\t additional argument: m or max to compute expertises only up to arg datetime, arg has to be in unix time (optional)");
            Console.WriteLine("\t Example of usage:");
            Console.WriteLine("\t AlgorithmRunner.exe Firefox Bugzilla C:\\ExpertiseExplorerOutput\\ a f r 1353412846\n");
        }
    }
}
