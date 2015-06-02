using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AlgorithmRunner.AbstractIssueTracker;

namespace AlgorithmRunner.Gerrit
{
    class GerritReviewFactory : ReviewInfoFactory
    {
        public string GerritCSVPath { get; protected set; }

        public GerritReviewFactory(string GerritCSVPath)
            : base(GerritCSVPath)
        {
            this.GerritCSVPath = GerritCSVPath;
        }

        public override IEnumerable<ReviewInfo> parseReviewInfos()
        {
            return File.ReadAllLines(GerritCSVPath)
                .Select(reviewCSVLine => new GerritReview(reviewCSVLine));
        }

    }
}
