using ExpertiseExplorer.Algorithms.Statistics;
using System;
using System.IO;

namespace ExpertiseExplorer.Statistics
{
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

            string sourceURL = args[0];
            string basepath = args[1];
            ExpertiseExplorer.Algorithms.Statistics.Statistics statistics = new ExpertiseExplorer.Algorithms.Statistics.Statistics(sourceURL, basepath);

            StatisticsOperation statisticsOperation;
            try
            {
                statisticsOperation = (StatisticsOperation)Enum.Parse(typeof(StatisticsOperation), args[2]);
            }
            catch (Exception)
            {
                Console.WriteLine("Error: operation needs to be an integer in the range from 0 to 4");
                return;
            }

            switch (args.Length)
            {
                case 3:
                    if (statisticsOperation != StatisticsOperation.FindMissingReviewers)
                    {
                        Console.WriteLine("Error: source argument is missing");
                        return;
                    }

                    statistics.FindMissingReviewers();

                    return;

                case 4:
                    if (StatisticsOperation.FindAliasesFromNames == statisticsOperation
                        || StatisticsOperation.FindAliasesFromAuthors == statisticsOperation)
                    {
                        string path2MappingFile = args[3];
                        if (!File.Exists(path2MappingFile))
                        {
                            Console.WriteLine("Error: the source argument must specify a path to the names file for operation FindAliasesFromX");
                            return;
                        }

                        if (StatisticsOperation.FindAliasesFromNames == statisticsOperation)
                            statistics.FindAliasesFromNames(path2MappingFile);
                        else
                            statistics.FindAliasesFromAuthors(path2MappingFile);
                    }
                    else
                    {
                        AbstractSourceOfBugs.StatisticsSource statisticsSource = (AbstractSourceOfBugs.StatisticsSource)Enum.Parse(typeof(AbstractSourceOfBugs.StatisticsSource), args[3]);
                        AbstractSourceOfBugs sourceOfActualReviews = AbstractSourceOfBugs.createSourceFromParameter(statisticsSource, statistics.RepositoryId);

                        switch (statisticsOperation)
                        {
                            case StatisticsOperation.AnalyzeActualReviews:
                                statistics.AnalyzeActualReviews(sourceOfActualReviews);
                                break;
                            case StatisticsOperation.ComputeStatisticsForAllAlgorithmsAndActualReviews:
                                statistics.ComputeStatisticsForAllAlgorithmsAndActualReviews(sourceOfActualReviews);
                                break;
                            case StatisticsOperation.FindIntersectingEntriesForAllAlgorithms:
                                statistics.FindIntersectingEntriesForActualReviewerIds(sourceOfActualReviews);
                                break;
                            case StatisticsOperation.FindIntersectingEntriesForAllAlgorithmsPairwise:
                                statistics.FindIntersectingEntriesPairwiseForActualReviewerIds(sourceOfActualReviews);
                                break;
                            default:
                                throw new NotImplementedException("The operation \"" + statisticsOperation + "\" has not been implemented");
                        }
                    }

                    break;
                default:
                    ShowHelp();
                    return;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Statistics.exe RepositoryURL Path Operation [Source]");
            Console.WriteLine("RepositoryURL: Which Repository should be analysed?");
            Console.WriteLine("Path: the path where all generated output will be saved");
            Console.WriteLine("Operation: the operation that should be executed");
            Console.WriteLine("\t Possible options:");
            Console.WriteLine("\t 0 - Find actual reviewers that are missing in the db (this operation needs no source argument)");
            Console.WriteLine("\t 1 - AnalyzeActualReviews");
            Console.WriteLine("\t 2 - ComputeStatisticsForAllAlgorithmsAndActualReviews");
            Console.WriteLine("\t 3 - FindIntersectingEntriesForAllAlgorithms");
            Console.WriteLine("\t 4 - FindIntersectingEntriesForAllAlgorithmsPairwise");
            Console.WriteLine("\t 5 - FindAliasesFromNames (this operation needs a special source argument)");
            Console.WriteLine("\t 6 - FindAliasesFromAuthors (this operation needs a special source argument)");
            Console.WriteLine("Source: source data and restrictions");
            Console.WriteLine("\t Possible options:");
            Console.WriteLine("\t 0 - All data (unfiltered)");
            Console.WriteLine("\t 1 - Use only reviews where 'hg@mozilla.com' is not identified as an expert");
            Console.WriteLine("\t 2 ID - Use only reviews of bugs with a database BugID of at least ID");
            Console.WriteLine("\t path - Only for FindAliasesFromX: Path to names/authors file");
        }
    }
}
