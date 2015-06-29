namespace ExpertiseExplorer.Common
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    [Serializable]
    public class Changeset
    {
        [NonSerialized]
        private readonly Dictionary<string, FileChange> fileDictionary;

        [NonSerialized]
        private readonly bool isMerge;

        public Changeset()
        {
            fileDictionary = new Dictionary<string, FileChange>();
        }

        public Changeset(StreamReader reader, string changesetIdentifier)
            : this()
        {
            Parents = string.Empty;
            var id = changesetIdentifier.Split(':')[0];
            Id = uint.Parse(id);
            Hash = changesetIdentifier.Split(':')[1];

            string input;
            while ((input = reader.ReadLine()) != null)
            {
                if (input.Contains("tag:"))
                {
                    Tag = input.Substring("tag:".Length).Trim();
                    continue;
                }

                if (input.Contains("parent:"))
                {
                    if (Parents == string.Empty)
                        Parents = input.Substring("parent:".Length).Trim();
                    else
                    {
                        Parents += " / " + input.Substring("parent:".Length).Trim();
                        isMerge = true;
                    }

                    continue;
                }

                if (input.Contains("user:"))
                {
                    User = input.Substring("user:".Length).Trim();
                    break;
                }
            }

            ReadDate(reader.ReadLine());

            if (reader.Peek() == 'f')
                ReadFiles(reader.ReadLine());

            ReadDescription(reader);
        }

        public string User { get; set; }

        public string Description { get; set; }

        public string Tag { get; set; }

        public string Parents { get; set; }

        public uint Id { get; set; }

        public string Hash { get; set; }

        public FileChange[] Files { get; set; }

        public DateTime Date { get; set; }

        public void ExtractDiffForFile(StreamReader reader, string filename)
        {
            if (!fileDictionary.ContainsKey(filename))
                fileDictionary.Add(filename, new FileChange(filename));

            var file = fileDictionary[filename];
            string line;
            var headerRead = false;
            var stringBuilder = new StringBuilder();
            while ((line = reader.ReadLine()) != null)
            {
                stringBuilder.AppendLine(line);

                if (!headerRead && line.Contains("---"))
                {
                    if (line.Contains("/dev/null"))
                    {
                        file.IsNew = true;
                    }

                    stringBuilder.AppendLine(reader.ReadLine()); // skip line with +++ header info
                    stringBuilder.AppendLine(reader.ReadLine()); // skip line with @@ header info
                    headerRead = true;
                    continue;
                }

                if (line.Length > 0)
                {
                    if (line[0] == '+')
                    {
                        file.LinesAdded++;
                    }

                    if (line[0] == '-')
                    {
                        file.LinesDeleted++;
                    }
                }

                // 'd'iff or 'c'hangeset or newline without ' '
                if (reader.Peek() == 'd' || reader.Peek() == 'c' || reader.Peek() == '\n')
                    break;
            }

            var stringToEncode = stringBuilder.ToString();
            if (stringToEncode == string.Empty)
            {
                throw new Exception("RawDiff is empty.");
            }

            var bytesToEncode = Encoding.ASCII.GetBytes(stringToEncode);
            file.RawDiff = Convert.ToBase64String(bytesToEncode);
        }

        public void FinalizeFileList()
        {
            Files = fileDictionary.Values.ToArray();
        }

        private void ReadDate(string dateline)
        {
            if (!dateline.Contains("date:"))
                throw new Exception("Invalid 'date' line read.");

            dateline = dateline.Substring("date:".Length).Trim();
            dateline = dateline.Substring(0, dateline.Length - 2);

            // Thu Mar 22 10:29:00 2007 -0700
            Date = DateTime.ParseExact(dateline, "ddd MMM dd HH:mm:ss yyyy zz", new CultureInfo("en-us", false).DateTimeFormat);
        }

        private void ReadFiles(string filesline)
        {
            if (!filesline.Contains("files:"))
                throw new Exception("Invalid 'files' line read.");

            // unreliable... why does mercurial allow whitespace in filenames but doesn't add delimiters to show where the actual filenames end?

            /*
            filesline = filesline.Substring("files:".Length).Trim();
            if (filesline.Contains(' '))
            {
                var filesToAdd = filesline.Split(' ');
                foreach (var filename in filesToAdd)
                {
                    if (!fileDictionary.ContainsKey(filename))
                    {
                        fileDictionary.Add(filename, new FileChange(filename));
                    }
                }
            }
            else
                if (!fileDictionary.ContainsKey(filesline))
                    fileDictionary.Add(filesline, new FileChange(filesline));
             */
        }

        private void ReadDescription(StreamReader reader)
        {
            // description:\n
            var line = reader.ReadLine();
            if (line != null && !line.Contains("description:")) 
                throw new Exception("Invalid 'description' line read.");

            var descriptionline = reader.ReadLine();

            while ((line = reader.ReadLine()) != null)
            {
                descriptionline += "\n" + line;

                // read until diff... or changeset...
                if (reader.Peek() == 'd' || reader.Peek() == 'c')
                {
                    Description = descriptionline.Trim();
                    break;
                }
            }
        }
    }
}
