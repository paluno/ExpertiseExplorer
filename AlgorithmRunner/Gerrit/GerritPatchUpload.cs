using ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace ExpertiseExplorer.AlgorithmRunner.Gerrit
{
    class GerritPatchUpload : PatchUpload
    {
        public GerritPatchUpload(string reviewLine)
        {
            string[] reviewValues = reviewLine.Split(';');

            ChangeId = reviewValues[2];
            When = DateTime.Parse(reviewValues[0], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();
            Filenames = reviewValues[3].Split(',').Select(filenameWithLineNumbers => parseFilename(filenameWithLineNumbers)).ToList();
        }

        private string parseFilename(string filenameWithLineNumbers)
        {
            return filenameWithLineNumbers.Split(':')[0];
        }

        public override bool isValid()
        {
            return Filenames.Any(str => !String.IsNullOrWhiteSpace(str));
        }

        public override string ToString()
        {
            return string.Join(";", When.ToUniversalTime().ToString("u"), "c", ChangeId, string.Join(",", Filenames));
        }
    }
}
