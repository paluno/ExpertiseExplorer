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
            return File.ReadAllLines(InputFilePath)
                .Select(reviewCSVLine => new GerritReview(reviewCSVLine));
        }

    }
}
