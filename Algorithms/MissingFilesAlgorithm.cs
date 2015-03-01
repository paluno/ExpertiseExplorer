namespace Algorithms
{
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

        public override void CalculateExpertise()
        {
            throw new System.NotImplementedException();
        }

        public bool CheckForMissingFiles(List<string> filenames)
        {
            var sometingIsWrong = false;
            foreach (var filename in filenames)
            {
                if (filename == string.Empty)
                    continue;

                if (foundFiles.ContainsKey(filename))
                    continue;

                if (missingFiles.ContainsKey(filename))
                {
                    missingFiles[filename] += 1;
                    sometingIsWrong = true;
                    continue;
                }

                if (ambiguousFiles.ContainsKey(filename))
                {
                    ambiguousFiles[filename] += 1;
                    sometingIsWrong = true;
                    continue;
                }

                var fileId = GetFilenameIdFromFilenameApproximation(filename);
                switch (fileId)
                {
                    case -1:
                        missingFiles.Add(filename, 1);
                        sometingIsWrong = true;
                        break;

                    case -2:
                        ambiguousFiles.Add(filename, 1);
                        sometingIsWrong = true;
                        break;

                    default:
                        foundFiles.Add(filename, 1);
                        break;
                }
            }

            return sometingIsWrong;
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
