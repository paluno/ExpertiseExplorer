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
    }
}
