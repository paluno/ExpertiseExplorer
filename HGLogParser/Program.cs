namespace HGLogParser
{
    using System;

    public class Program
    {
        // HgLogParser name pathToDiffFile [[pathToOutputFolder] [numberOfChangesetsPerXMLFile]]
        public static void Main(string[] args)
        {
            HgLogParser parser;
            int numberOfChangesetsPerFile;

            switch (args.Length)
            {
                case 2:
                    parser = new HgLogParser(args[0], args[1]);
                    break;

                case 3:
                    parser = int.TryParse(args[1], out numberOfChangesetsPerFile) ? new HgLogParser(args[0], args[1], null, numberOfChangesetsPerFile) : new HgLogParser(args[0], args[1], args[2]);
                    break;
                
                case 4:
                    if (!int.TryParse(args[3], out numberOfChangesetsPerFile))
                    {
                        Console.WriteLine("Error parsing: " + args[3]);
                        ShowHelp();
                        return;
                    }

                    if (numberOfChangesetsPerFile < 1)
                    {
                        Console.WriteLine("numberOfChangesetsPerXMLFile needs to be a positive integer");
                        return;
                    }

                    parser = new HgLogParser(args[0], args[1], args[2], numberOfChangesetsPerFile);
                    break;
                
                default:
                    ShowHelp();
                    return;
            }

            Console.Write("Processing...");
            parser.Run();
            Console.WriteLine("Finished!");
        }

        private static void ShowHelp()
        {
            Console.WriteLine("HGLogParser.exe name pathToDiffFile [[pathToOutputFolder] [numberOfChangesetsPerXMLFile]]");
        }
    }
}
