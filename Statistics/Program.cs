﻿namespace Statistics
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

            string sourceURL = args[0];
            string basepath = args[1];
            Statistics statistics = new Statistics(sourceURL, basepath);

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

                    break;

                case 4:
                    SourceOfActualReviewers.StatisticsSource statisticsSource = (SourceOfActualReviewers.StatisticsSource)Enum.Parse(typeof(SourceOfActualReviewers.StatisticsSource), args[3]);
                    SourceOfActualReviewers sourceOfActualReviews = SourceOfActualReviewers.createSourceFromParameter(statisticsSource, statistics.RepositoryId);

                    switch(statisticsOperation)
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
            Console.WriteLine("Source: source data and restrictions");
            Console.WriteLine("\t Possible options:");
            Console.WriteLine("\t 0 - All data (unfiltered)");
            Console.WriteLine("\t 1 - Use only reviews where 'hg@mozilla.com' is not identified as an expert");
            Console.WriteLine("\t 2 - Use only reviews with only one associated artifact");
        }
    }
}
