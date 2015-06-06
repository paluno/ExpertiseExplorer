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

                string typ = line.Split(';')[1];

                if (typ == "r")
                    result.Add(new GerritReview(line));
                else if (typ == "c")
                    result.Add(new GerritPathUpload(line));
                else
                    throw new SystemException("Unknown Gerrit-Typ: " + typ);

            }
            return result;
        }

    }
}
