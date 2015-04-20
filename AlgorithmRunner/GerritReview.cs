using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner
{
    class GerritReview : ReviewInfo
    {
        public GerritReview(string reviewLine)
        {
            string[] reviewValues = reviewLine.Split(';');

            ChangeId = reviewValues[1];
            When = DateTime.Parse(reviewValues[2]);
            Reviewer = reviewValues[8];
            Filenames = reviewValues[6].Split(',').Select(filenameWithLineNumbers => parseFilename(filenameWithLineNumbers)).ToList();
            ActivityId = reviewValues[5].GetHashCode(); // revisionId
        }

        private string parseFilename(string filenameWithLineNumbers)
        {
            return filenameWithLineNumbers.Split(':')[0];
        }
    }
}
