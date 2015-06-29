using ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.AlgorithmRunner.Gerrit
{
    class GerritPatchUpload : PatchUpload
    {
        public GerritPatchUpload(string reviewLine)
        {
            string[] reviewValues = reviewLine.Split(';');

            ChangeId = reviewValues[2];
            When = DateTime.Parse(reviewValues[0]);
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
    }
}
