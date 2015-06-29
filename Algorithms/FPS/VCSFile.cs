using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms.FPS
{
    internal class VCSFile : VCSObject
    {
        /// <summary>
        /// Result of the WeighedReviewCountAlgorithm: Which developers have reviewed this file already? And how often, according to FPS?
        /// </summary>
        public IDictionary<int, double> WeighedDeveloperExpertise { get; private set; }

        public VCSFile(string name)
            : base(name)
        {
            WeighedDeveloperExpertise = new Dictionary<int, double>(10);
        }

        public override void AddReview(int idReviewer, IEnumerable<string> filenameComponents, double reviewWeight)
        {
            if (WeighedDeveloperExpertise.ContainsKey(idReviewer))
                WeighedDeveloperExpertise[idReviewer] += reviewWeight;
            else
                WeighedDeveloperExpertise.Add(idReviewer, reviewWeight);
        }

        internal override void CalculateDeveloperExpertises(System.Collections.Concurrent.ConcurrentDictionary<int, double> dictExpertises, string[] filenameComponents, int currentDepth, int numberOfMatchingComponents)
        {
            int maxLength = Math.Max(filenameComponents.Length, currentDepth);
            int matchLength = numberOfMatchingComponents;   // there will be some difference, unless ...
            if (currentDepth - 1 == matchLength && currentDepth == filenameComponents.Length && filenameComponents[currentDepth - 1] == RelativeName)
                ++matchLength;                              // ... unless it's an edit of the file itself

            double fileSimilarity = matchLength / (double)maxLength;

            foreach(KeyValuePair<int,double> weighedExpertise in WeighedDeveloperExpertise)
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
