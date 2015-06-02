using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner
{
    class GerritReview : ReviewInfo
    {
        public string RevisionId { get; private set; }

        public GerritReview(string reviewLine)
        {
            string[] reviewValues = reviewLine.Split(';');

            ChangeId = reviewValues[1];
            When = DateTime.Parse(reviewValues[2]);
            Reviewer = reviewValues[8];
            Filenames = reviewValues[6].Split(',').Select(filenameWithLineNumbers => parseFilename(filenameWithLineNumbers)).ToList();
            RevisionId = reviewValues[5];
            ActivityId = reviewValues[5].GetHashCode();
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
            return ";" + ChangeId + ";" + When + ";;;" + RevisionId + ";" + string.Join(",", Filenames) + ";;" + Reviewer;
        }
    }
}
