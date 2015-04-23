using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms.FPS
{
    class VCSDirectory : VCSObject
    {
        /// <summary>
        /// All subdirectories and the files directly in this directory
        /// </summary>
        public IDictionary<string, VCSObject> Children { get; private set; }

        public VCSDirectory(string relativeName)
            : base(relativeName)
        {
            this.Children = new Dictionary<string, VCSObject>(10);
        }

        public override void AddReview(string reviewer, IEnumerable<string> filenameComponents, double reviewWeight)
        {
            string nextComponent = filenameComponents.First();
            IEnumerable<string> remainingComponents = filenameComponents.Skip(1);

            if (!Children.ContainsKey(nextComponent))
                if (remainingComponents.Count() > 1)        // it's a directory
                    Children.Add(nextComponent, new VCSDirectory(nextComponent));
                else        // it's a file
                    Children.Add(nextComponent, new VCSFile(nextComponent));

            Children[nextComponent].AddReview(reviewer, remainingComponents, reviewWeight);
        }

        internal override void CalculateDeveloperExpertises(ConcurrentDictionary<string, double> dictExpertises, string[] filenameComponents, int currentDepth, int numberOfMatchingComponents)
        {
            int numberOfStillMatchingComponents;
            if (currentDepth - 1 == numberOfMatchingComponents && RelativeName == filenameComponents[currentDepth - 1]) // still matching
                numberOfStillMatchingComponents = numberOfMatchingComponents + 1;
            else
                numberOfStillMatchingComponents = numberOfMatchingComponents;

            Parallel.ForEach(
                Children,
                kvp => CalculateDeveloperExpertises(dictExpertises, filenameComponents, currentDepth + 1, numberOfStillMatchingComponents)
            );
        }
    }
}
