using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms.FPS
{
    class VCSFile : VCSObject
    {
        /// <summary>
        /// Result of the WeighedReviewCountAlgorithm: Which developers have reviewed this file already? And how often, according to FPS?
        /// </summary>
        public IDictionary<string, double> WeighedDeveloperExpertise { get; private set; }

        public VCSFile(string name)
            : base(name)
        {
            WeighedDeveloperExpertise = new Dictionary<string, double>(10);
        }

        public override void AddReview(string reviewer, IEnumerable<string> filenameComponents, double reviewWeight)
        {
            if (WeighedDeveloperExpertise.ContainsKey(reviewer))
                WeighedDeveloperExpertise[reviewer] += reviewWeight;
            else
                WeighedDeveloperExpertise.Add(reviewer, reviewWeight);
        }

        internal override void CalculateDeveloperExpertises(System.Collections.Concurrent.ConcurrentDictionary<string, double> dictExpertises, string[] filenameComponents, int currentDepth, int numberOfMatchingComponents)
        {
            int maxLength = Math.Max(filenameComponents.Length, currentDepth);
            int matchLength = numberOfMatchingComponents;   // there will be some difference, unless ...
            if (currentDepth - 1 == matchLength && currentDepth == filenameComponents.Length && filenameComponents[currentDepth - 1] == RelativeName)
                ++matchLength;                              // ... unless it's an edit of the file itself

            double fileSimilarity = matchLength / (double)maxLength;

            foreach(KeyValuePair<string,double> weighedExpertise in WeighedDeveloperExpertise)
            {
                double weighedValue = weighedExpertise.Value * fileSimilarity;
                dictExpertises.AddOrUpdate(
                    weighedExpertise.Key,       // the developer name
                    weighedValue,               // it may be the first value
                    (key, currentValue) => currentValue + weighedValue  // Otherwise, add it to the existing value
                );
            }
        }
    }
}
