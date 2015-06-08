using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlgorithmRunner.AbstractIssueTracker;

namespace AlgorithmRunner.Gerrit
{
    class GerritReviewFactory : IssueTrackerEventFactory
    {
        public GerritReviewFactory(string GerritCSVPath)
            : base(GerritCSVPath)
        {
        }

        public override IEnumerable<IssueTrackerEvent> parseIssueTrackerEvents()
        {
            List<IssueTrackerEvent> result = new List<IssueTrackerEvent>();
            string[] lines = File.ReadAllLines(InputFilePath);

            foreach (string line in lines)
            {

                string type = line.Split(';')[1];

                if (type == "r")
                    result.Add(new GerritReview(line));
                else if (type == "c")
                    result.Add(new GerritPatchUpload(line));
                else
                    throw new SystemException("Unknown Gerrit-Type: " + type);

            }
            return result;
        }

    }
}
