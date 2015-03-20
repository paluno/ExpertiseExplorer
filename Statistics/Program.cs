namespace Statistics
{
    using System;

    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                ShowHelp();
                return;
            }

            var basepath = args[0];
            var statistics = new Statistics(basepath);

            var operation = args[1];
            var statisticsOperation = GetOperationFromString(operation);
            if (statisticsOperation == StatisticsOperation.InvalidOperation)
            {
                Console.WriteLine("Error: operation needs to be an integer in the range from 0 to 4");
                return;
            }

            switch (args.Length)
            {
                case 2:
                    if (statisticsOperation != StatisticsOperation.FindMissingReviewers)
                    {
                        Console.WriteLine("Error: source argument is missing");
                        return;
                    }

                    statistics.Run(statisticsOperation);

                    break;

                case 3:
                    var source = args[2];
                    var statisticsSource = GetSourceFromString(source);
                    if (statisticsSource == StatisticsSource.None)
                    {
                        Console.WriteLine("Error: source needs to be an integer in the range from 0 to 2");
                        return;
                    }

                    statistics.Run(statisticsOperation, statisticsSource);

                    break;

                default:
                    ShowHelp();
                    return;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Statistics.exe path operation [source]");
            Console.WriteLine("path: the path where all generated output will be saved");
            Console.WriteLine("operation: the operation that should be executed");
            Console.WriteLine("\t possible options:");
            Console.WriteLine("\t 0 - Find actual reviewers that are missing in the db (this operation needs no source argument)");
            Console.WriteLine("\t 1 - AnalyzeActualReviews");
            Console.WriteLine("\t 2 - ComputeStatisticsForAllAlgorithmsAndActualReviews");
            Console.WriteLine("\t 3 - FindIntersectingEntriesForAllAlgorithms");
            Console.WriteLine("\t 4 - FindIntersectingEntriesForAllAlgorithmsPairwise");
            Console.WriteLine("source: source data and restrictions");
            Console.WriteLine("\t possible options:");
            Console.WriteLine("\t 0 - All data (unfiltered)");
            Console.WriteLine("\t 1 - Use only reviews where 'hg@mozilla.com' is not identified as an expert");
            Console.WriteLine("\t 2 - Use only reviews with only one associated artifact");
        }

        private static StatisticsOperation GetOperationFromString(string argument)
        {
            int operation;
            if (!int.TryParse(argument, out operation))
                return StatisticsOperation.InvalidOperation;

            if (operation < 0 || operation > 4)
                return StatisticsOperation.InvalidOperation;

            return (StatisticsOperation)operation;
        }

        private static StatisticsSource GetSourceFromString(string argument)
        {
            int source;
            if (!int.TryParse(argument, out source))
                return StatisticsSource.None;

            if (source < 0 || source > 2)
                return StatisticsSource.None;

            return (StatisticsSource)source;
        }
    }
}
