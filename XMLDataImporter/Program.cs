namespace XMLDataImporter
{
    using System;

    public class Program
    {
        // XMLDataImporter pathToXMLFile(s)
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                ShowHelp();
                return;
            }

            var xmlDataImporter = new XmlDataImporter();
            xmlDataImporter.ProcessDirectory(args[0]);
        }

        public static void ShowHelp()
        {
            Console.WriteLine("Usage: XMLDataImporter.exe pathToXMLFile(s)");
        }
    }
}
