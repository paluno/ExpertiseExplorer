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
            : base(sourceUrl, basepath,
                new AlgorithmBase[]
                {
                    new WeighedReviewCountAlgorithm(),
                    new FPSReviewAlgorithm()
                })
        {
        }

        protected override void ProcessReviewInfo(ReviewInfo info, IList<string> involvedFiles, System.IO.StreamWriter found, System.Diagnostics.Stopwatch stopwatch)
        {
            base.ProcessReviewInfo(info, involvedFiles, found, stopwatch);

            // Calculate new values for reviewer scores
//            if (info.IsReview)
                foreach (ReviewAlgorithmBase reviewAlgorithm in Algorithms.OfType<ReviewAlgorithmBase>())
                    reviewAlgorithm.AddReviewScore(info.Reviewer, involvedFiles);
        }
    }
}
