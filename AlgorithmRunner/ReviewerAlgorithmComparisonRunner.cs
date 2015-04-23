using Algorithms;
using Algorithms.FPS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner
{
    internal class ReviewerAlgorithmComparisonRunner : AlgorithmComparisonRunner
    {
        RootDirectory FpsTree { get; set; }

        private ReviewerAlgorithmComparisonRunner(string sourceUrl, string basepath, RootDirectory fpsTree)
            : base(sourceUrl, basepath,
                new AlgorithmBase[]
                {
                    new WeighedReviewCountAlgorithm(fpsTree),
                    new FPSReviewAlgorithm(fpsTree)
                })
        {
            FpsTree = fpsTree;
        }

        public ReviewerAlgorithmComparisonRunner(string sourceUrl, string basepath)
            : this(sourceUrl, basepath, new RootDirectory())
        {
        }

        public void InitFromDB()
        {
            Algorithms.OfType<WeighedReviewCountAlgorithm>().First().LoadReviewScoresFromDB();
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
