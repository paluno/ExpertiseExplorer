using Algorithms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner
{
    internal class ReviewerAlgorithmComparisonRunner : AlgorithmComparisonRunner
    {
        public ReviewerAlgorithmComparisonRunner(string sourceUrl, string basepath)
            : base(sourceUrl, basepath)
        {

        }

        protected override void InitAlgorithms(string sourceUrl)
        {
            Algorithms = new AlgorithmBase[]
            {
                new WeighedReviewCountAlgorithm(),
                new FPSReviewAlgorithm()
            };

            // Load Ids from DB for first algorithm, gets set for all other later
            Algorithms[0].InitIdsFromDbForSourceUrl(sourceUrl, false);
        }

        protected override void ProcessActivityInfo(ActivityInfo info, IList<string> involvedFiles, System.IO.StreamWriter found, System.Diagnostics.Stopwatch stopwatch)
        {
                // Calculate new values for reviewer scores
            if (info.IsReview)
                foreach (ReviewAlgorithmBase reviewAlgorithm in Algorithms.OfType<ReviewAlgorithmBase>())
                    reviewAlgorithm.AddReviewScore(info.Author,involvedFiles);

            base.ProcessActivityInfo(info, involvedFiles, found, stopwatch);
        }
    }
}
