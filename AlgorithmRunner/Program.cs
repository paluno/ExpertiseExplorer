namespace AlgorithmRunner
{
    using System;

    public class Program
    {
        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            if (args.Length < 3)
            {
                ShowHelp();
                return;
            }

            var sourceUrlIdentifier = args[0];
            var basepath = args[1];
            var mode = args[2].ToLower();

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
                    var forceOverwrite = false;
                    var noComp = false;
                    int timeOfLastComparison = 0;
                    int timeOfMaxComparison = int.MaxValue;
                    AlgorithmComparisonRunner comparisonRunner;
                    if ("review" == mode)
                        comparisonRunner = new ReviewerAlgorithmComparisonRunner(sourceUrlIdentifier, basepath);
                    else                       
                        comparisonRunner = new AlgorithmComparisonRunner(sourceUrlIdentifier, basepath);    
                
                    if (args.Length == 3)
                    {
                        comparisonRunner.PrepareInput(basepath + "input.txt", basepath + "input_final.txt");
                        comparisonRunner.StartComparisonFromFile(basepath + @"input_final.txt", DateTime.MinValue, DateTime.MaxValue);
                    }
                    else
                    {
                        for (int i = 3; i < args.Length; i++)
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

                        DateTime resumeTime = ActivityInfo.UnixTime2PDTDateTime(timeOfLastComparison)
                            - new TimeSpan(0, 0, 0, 1);
                        DateTime maxTime = ActivityInfo.UnixTime2PDTDateTime(timeOfMaxComparison);

                        comparisonRunner.PrepareInput(basepath + "input.txt", basepath + "input_final.txt", forceOverwrite);
                        comparisonRunner.StartComparisonFromFile(basepath + @"input_final.txt", resumeTime, maxTime, noComp);
                    }

                    return;

                default:
                    ShowHelp();
                    break;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("AlgorithmRunner.exe name path mode [arg0....argN]");
            Console.WriteLine("name: the sourceRepository.sourceUrl identifier");
            Console.WriteLine("path: the path where the crawler output is saved and where all generated output will be saved");
            Console.WriteLine("mode: the operating mode, can be:");
            Console.WriteLine("\t c or clean for filename cleanup of crawled filenames (filters everything that does not belong to the Firefox project e.g. files from camino, wallet)");
            Console.WriteLine("\t additional argument: none\n");
            Console.WriteLine("\t a or algorithm for algorithm comparison");
            Console.WriteLine("\t review is also algorithm comparision, but using the review algorithm set instead of the usual algorithms");
            Console.WriteLine("\t additional argument: f or force for forcing an existing prepared input to be overwritten (optional)");
            Console.WriteLine("\t additional argument: n or nocomp for only creating expertise values from revision, skipping the comparison (optional)");
            Console.WriteLine("\t additional argument: r or resume arg for resuming the computation from arg datetime, arg has to be in unix time (optional)");
            Console.WriteLine("\t \t Useful for resuming the calculations after an error. Use repository.lastUpdate as the value for arg\n");
            Console.WriteLine("\t additional argument: m or max to compute expertises only up to arg datetime, arg has to be in unix time (optional)");
            Console.WriteLine("\t Example of usage:");
            Console.WriteLine("\t AlgorithmRunner.exe Firefox C:\\ExpertiseExplorerOutput\\ a f r 1353412846\n");
        }
    }
}
