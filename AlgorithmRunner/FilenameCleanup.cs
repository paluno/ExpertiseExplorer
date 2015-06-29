namespace ExpertiseExplorer.AlgorithmRunner
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    internal class FilenameCleanup
    {
        public void StartCleanup(string inFile, string outFile)
        {
            var attachments = new List<Attachment>();

            var attachmentLines = File.ReadLines(inFile);
            foreach (var attachmentLine in attachmentLines)
            {
                var attachment = new Attachment
                    {
                        BugId = int.Parse(attachmentLine.Split(';')[0]),
                        AttachmentId = int.Parse(attachmentLine.Split(';')[1]),
                    };

                var files = attachmentLine.Split(';')[2].Split(',').ToList();
                files = FilterDublicates(files);

                if (!(IsMailRelated(files) || IsSuiteRelated(files) || IsWebtoolsRelated(files) || IsWalletRelated(files) || IsCaminoRelated(files) || IsCalendarRelated(files) || IsBugzillaRelated(files)))
                {
                    attachment.Filenames = files;
                }   

                attachments.Add(attachment);
            }

            var sb = new StringBuilder();
            foreach (var attachment in attachments)
            {
                var line = attachment.BugId + ";" + attachment.AttachmentId + ";" + string.Join(",", attachment.Filenames);
                sb.AppendLine(line);
            }

            File.WriteAllText(outFile, sb.ToString());
        }

        private List<string> FilterDublicates(IEnumerable<string> files)
        {
            var result = new HashSet<string>();

            foreach (var file in files)
            {
                var workitem = file;
                if (workitem.StartsWith("a/"))
                {
                    workitem = workitem.Substring("a/".Length);
                }

                if (workitem.StartsWith("mozilla/"))
                {
                    result.Add(workitem.Substring("mozilla/".Length));
                }
                else if (workitem.StartsWith("/"))
                {
                    result.Add(workitem.Substring("/".Length));
                }
                else
                {
                    result.Add(workitem);
                }
            }

            return result.ToList();
        }

        private bool IsMailRelated(IEnumerable<string> filenames)
        {
            return filenames.Count(filename => filename.StartsWith("mail/") || filename.StartsWith("mailnews/")) > 0;
        }

        private bool IsSuiteRelated(IEnumerable<string> filenames)
        {
            return filenames.Count(filename => filename.StartsWith("suite/")) > 0;
        }

        private bool IsWebtoolsRelated(IEnumerable<string> filenames)
        {
            return filenames.Count(filename => filename.StartsWith("webtools/")) > 0;
        }

        private bool IsWalletRelated(IEnumerable<string> filenames)
        {
            return filenames.Count(filename => filename.Contains("/wallet/")) > 0;
        }

        private bool IsCaminoRelated(IEnumerable<string> filenames)
        {
            return filenames.Count(filename => filename.StartsWith("camino/")) > 0;
        }

        private bool IsCalendarRelated(IEnumerable<string> filenames)
        {
            return filenames.Count(filename => filename.StartsWith("calendar/")) > 0;
        }

        private bool IsBugzillaRelated(IEnumerable<string> filenames)
        {
            return filenames.Count(filename => filename.Contains("Bugzilla/")) > 0;
        }

        private class Attachment
        {
            public Attachment()
            {
                Filenames = new List<string>();
            }

            public int BugId { get; set; }

            public int AttachmentId { get; set; }

            public List<string> Filenames { get; set; }
        }
    }
}
