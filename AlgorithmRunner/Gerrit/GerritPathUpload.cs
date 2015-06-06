using AlgorithmRunner.AbstractIssueTracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner.Gerrit
{
    class GerritPathUpload : PatchUpload
    {
        public GerritPathUpload(string reviewLine)
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
    }
}
