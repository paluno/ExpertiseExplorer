namespace Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public class MissingFilesAlgorithm : AlgorithmBase
    {
        private readonly Dictionary<string, int> foundFiles = new Dictionary<string, int>();
        private readonly Dictionary<string, int> missingFiles = new Dictionary<string, int>();
        private readonly Dictionary<string, int> ambiguousFiles = new Dictionary<string, int>();

        public override void CalculateExpertiseForFile(string filename)
        {
            throw new System.NotImplementedException();
        }

        public bool CheckForMissingFiles(List<string> filenames)
        {
            bool somethingIsWrong = false;
            foreach (string filename in filenames)
            {
                if (filename == string.Empty)
                    continue;

                if (foundFiles.ContainsKey(filename))
                    continue;

                if (missingFiles.ContainsKey(filename))
                {
                    missingFiles[filename] += 1;
                    somethingIsWrong = true;
                    continue;
                }

                if (ambiguousFiles.ContainsKey(filename))
                {
                    ambiguousFiles[filename] += 1;
                    somethingIsWrong = true;
                    continue;
                }

                int fileId;
                try
                {
                    fileId = GetFilenameIdFromFilenameApproximation(filename);
                    foundFiles.Add(filename, 1);
                }
                catch (ArgumentException ae)
                {
                    if (ae.ParamName != "filename")
                        throw;
                    missingFiles.Add(filename, 1);
                    somethingIsWrong = true;
                }
                catch (InvalidOperationException)   // more than one file found
                {
                    ambiguousFiles.Add(filename, 1);
                    somethingIsWrong = true;
                }
            }

            return somethingIsWrong;
        }

        public void OutputMissingFilesTo(string filename)
        {
            var sw = new StringWriter();
            sw.WriteLine("---- stats {0} / {1} ----", missingFiles.Count, foundFiles.Count);
            sw.WriteLine("---- missingFiles ----");
            foreach (var kvp in missingFiles)
            {
                sw.WriteLine(kvp.Key + ";" + kvp.Value);
            }

            File.WriteAllText(filename, sw.ToString());
        }

        public void OutputAmbiguousFilesTo(string filename)
        {
            var sw = new StringWriter();
            sw.WriteLine("---- stats {0} / {1} ----", ambiguousFiles.Count, foundFiles.Count);
            sw.WriteLine("---- ambiguousFiles ----");
            foreach (var kvp in ambiguousFiles)
            {
                sw.WriteLine(kvp.Key + ";" + kvp.Value);
            }

            File.WriteAllText(filename, sw.ToString());
        }
    }
}
