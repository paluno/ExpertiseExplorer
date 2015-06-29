using ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.AlgorithmRunner.Gerrit
{
    class GerritReview : ReviewInfo
    {

        public GerritReview(string reviewLine)
        {
            string[] reviewValues = reviewLine.Split(';');

            ChangeId = reviewValues[2];
            When = DateTime.Parse(reviewValues[0]);
            Reviewer = reviewValues[4];
            Filenames = reviewValues[3].Split(',').Select(filenameWithLineNumbers => parseFilename(filenameWithLineNumbers)).ToList();
            ActivityId = Int16.Parse(reviewValues[5]);
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
            return When + ";r;" + ChangeId + ";" + string.Join(",", Filenames) + ";" + Reviewer + ";" + ActivityId;
        }
    }
}
