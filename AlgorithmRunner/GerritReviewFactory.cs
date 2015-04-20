using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner
{
    class GerritReviewFactory : ReviewInfoFactory
    {
        public string GerritCSVPath { get; protected set; }

        public GerritReviewFactory(string GerritCSVPath)
        {
            this.GerritCSVPath = GerritCSVPath;
        }

        public override IEnumerable<ReviewInfo> parseReviewInfos()
        {
            IDictionary<DateTime, ICollection<GerritReview>> dictReviews = new Dictionary<DateTime, ICollection<GerritReview>>(20000);

            string[] allReviewLines = File.ReadAllLines(GerritCSVPath);

            foreach (string reviewLine in allReviewLines)
            {
                GerritReview currentReview = new GerritReview(reviewLine);
                if (!dictReviews.ContainsKey(currentReview.When))
                    dictReviews.Add(currentReview.When, new LinkedList<GerritReview>());
                dictReviews[currentReview.When].Add(currentReview);
            }

            List<GerritReview> resultList = new List<GerritReview>();
            foreach (ICollection<GerritReview> nextReviewCollection in dictReviews.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value))
                resultList.AddRange(nextReviewCollection);

            return resultList;
        }
    }
}
