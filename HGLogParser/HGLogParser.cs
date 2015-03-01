namespace HGLogParser
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using ExpertiseExplorerCommon;

    public class HgLogParser
    {
        private const string CHANGESET_IDENTIFIER_PREFIX = "changeset:";

        private const string DIFF_LINE_PREFIX = "diff -";

        private readonly string source;
        private readonly string inputPath;
        private readonly string outputPath;
        private readonly int numberOfChangesetsPerFile;
        private readonly List<Changeset> changesets;

        public HgLogParser(string name, string pathToInputFile, string pathToOutputFolder = null, int maxPerFile = int.MaxValue)
        {
            source = name;
            inputPath = pathToInputFile;
            outputPath = pathToOutputFolder;
            numberOfChangesetsPerFile = maxPerFile;
            changesets = new List<Changeset>();
        }

        public void Run()
        {
            var input = new StreamReader(inputPath);
            Debug.WriteLine("Starting parsing at: " + DateTime.Now);
            try
            {
                ParseDump(input);
            }
            finally
            {
                input.Close();
            }

            Debug.WriteLine("Finished parsing at: " + DateTime.Now);

            HandleOutput();
        }

        private void ParseDump(StreamReader reader)
        {
            string line;
            Changeset lastChangeset = null;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(CHANGESET_IDENTIFIER_PREFIX))
                {
                    var changesetIdentifier = line.Substring(CHANGESET_IDENTIFIER_PREFIX.Length).Trim();
                    lastChangeset = new Changeset(reader, changesetIdentifier);
                    changesets.Add(lastChangeset);
                    continue;
                }

                if (line.StartsWith(DIFF_LINE_PREFIX))
                {
                    var filename = string.Empty;
                    var fields = line.Split(' ');
                    if (fields.Length > 6)
                    {
                        for (var i = 5; i < fields.Length; i++)
                        {
                            filename += fields[i] + " ";
                        }

                        filename = filename.TrimEnd();
                    }
                    else
                        filename = line.Split(' ').Last();

                    Debug.Assert(lastChangeset != null, "lastChangeset != null");
                    lastChangeset.ExtractDiffForFile(reader, filename);
                }
            }

            foreach (var changeset in changesets)
            {
                changeset.FinalizeFileList();
                foreach (var file in changeset.Files)
                {
                    if (file.RawDiff == null)
                    {
                        file.IsBinary = true;
                        Debug.WriteLine("Diff empty for file: '" + file.Name + "' in changeset: " + changeset.Id);
                    }
                }
            }
        }

        private void HandleOutput()
        {
            var formatter = new XmlSerializer(typeof(RepositoryImage));
            string path = (outputPath ?? string.Empty) + DateTime.Now.Ticks;

            if (numberOfChangesetsPerFile != int.MaxValue)
            {
                var numberOfFiles = changesets.Count() / numberOfChangesetsPerFile;
                var remainder = changesets.Count() % numberOfChangesetsPerFile;

                Debug.WriteLine("Starting Serialization at: " + DateTime.Now);

                int i;
                for (i = 0; i < numberOfFiles; i++)
                {
                    var filePath = path + "_" + i.ToString("000000") + ".xml";
                    var output = new FileStream(filePath, FileMode.Create);
                    var repository = new RepositoryImage(source)
                        {
                            Changesets = changesets.GetRange(i * numberOfChangesetsPerFile, numberOfChangesetsPerFile).ToArray()
                        };
                    formatter.Serialize(output, repository);
                    output.Close();
                }

                if (remainder > 0)
                {
                    var filePath = path + "_" + i.ToString("000000") + ".xml";
                    var output = new FileStream(filePath, FileMode.Create);
                    var repository = new RepositoryImage(source)
                        {
                            Changesets = changesets.GetRange(i * numberOfChangesetsPerFile, remainder).ToArray()
                        };
                    formatter.Serialize(output, repository);
                    output.Close();
                }
            }
            else
            {
                path += ".xml";
                var output = new FileStream(path, FileMode.Create);

                Debug.WriteLine("Starting Serialization at: " + DateTime.Now);

                var repository = new RepositoryImage(source) { Changesets = changesets.ToArray() };
                formatter.Serialize(output, repository);
                output.Close();
            }

            Debug.WriteLine("Finished Serialization at: " + DateTime.Now);
        }
    }
}
