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
                    var forceOverwrite = false;
                    var noComp = false;
                    var timeOfLastComparison = 0;
                    var comparisonRunner = new AlgorithmComparisonRunner(sourceUrlIdentifier, basepath);    
                
                    if (args.Length == 3)
                    {
                        comparisonRunner.PrepareInput(basepath + "input.txt", basepath + "input_final.txt");
                        comparisonRunner.StartComparisonFromFile(basepath + @"input_final.txt", DateTime.MinValue);
                    }
                    else
                    {
                        for (int i = 2; i < args.Length; i++)
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

                                    if (i == args.Length - 1)
                                    {
                                        Console.WriteLine("Error: resume argument is missing the parameter.");
                                        return;
                                    }

                                    if (!int.TryParse(args[i + 1], out timeOfLastComparison))
                                    {
                                        Console.WriteLine("Error: Unable to parse {0} as int.", args[i + 1]);
                                        return;
                                    }

                                    i++;
                                    break;

                                default:
                                    Console.WriteLine("Error: Unknown argument {0}.", args[i]);
                                    break;
                            }
                        }


                        var resumeTime = new ActivityInfo();
                        resumeTime.SetDateTimeFromUnixTime(timeOfLastComparison);
                        resumeTime.When = resumeTime.When - new TimeSpan(0, 0, 0, 1);

                        comparisonRunner.PrepareInput(basepath + "input.txt", basepath + "input_final.txt", forceOverwrite);
                        comparisonRunner.StartComparisonFromFile(basepath + @"input_final.txt", resumeTime.When, noComp);
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
            Console.WriteLine("\t additional argument: f or force for forcing an existing prepared input to be overwritten (optional)");
            Console.WriteLine("\t additional argument: n or nocomp for only reating expertise values from revision, skipping the comparison (optional)");
            Console.WriteLine("\t additional argument: r or resume arg for resuming the computation from arg datetime, arg has to be in unix time (optional)");
            Console.WriteLine("\t \t Useful for resuming the calculations after an error. Use repository.lastUpdate as the value for arg\n");
            Console.WriteLine("\t Example of usage:");
            Console.WriteLine("\t AlgorithmRunner.exe Firefox C:\\ExpertiseExplorerOutput\\ a f r 1353412846\n");
        }
    }
}
