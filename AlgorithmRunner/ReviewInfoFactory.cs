using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner
{
    /// <summary>
    /// Abstract Factory
    /// </summary>
    abstract class ReviewInfoFactory
    {
        public string InputFilePath { get; set; }

        abstract public IEnumerable<ReviewInfo> parseReviewInfos();

        protected ReviewInfoFactory(string inputFilePath)
        {
            this.InputFilePath = inputFilePath;
        }

        /// <summary>
        /// parses and filters a raw input file and writes back a chronologically ordered file
        /// </summary>
        public void PrepareInput(string pathToRawInputFile, bool overwrite = false)
        {
            if (!overwrite && File.Exists(InputFilePath))
                return;

            PrefilterRawInput(pathToRawInputFile);

            IEnumerable<ReviewInfo> list = parseReviewInfos();

            // ordering of & another filter pass on the reviews
            IDictionary<DateTime, ICollection<ReviewInfo>> dictReviews = new Dictionary<DateTime, ICollection<ReviewInfo>>(20000);
            foreach (ReviewInfo currentReview in list)
            {
                if (!currentReview.isValid())
                    continue;

                if (!dictReviews.ContainsKey(currentReview.When))
                    dictReviews.Add(currentReview.When, new LinkedList<ReviewInfo>());
                dictReviews[currentReview.When].Add(currentReview);
            }

            // list is ordered by whatever, maybe ChangeId, but not datetime
            var sb = new StringBuilder();
            foreach (ICollection<ReviewInfo> nextReviewCollection in dictReviews.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value))
                foreach (ReviewInfo ri in nextReviewCollection)
                    sb.AppendLine(ri.ToString());

            Debug.WriteLine("Finished ordering at: " + DateTime.Now);

            File.WriteAllText(InputFilePath, sb.ToString());
        }

        protected virtual void PrefilterRawInput(string pathToRawInputFile)
        {
            File.Copy(pathToRawInputFile, InputFilePath);
        }
    }
}
