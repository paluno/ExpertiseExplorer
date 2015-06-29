using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms.FPS
{
    public class VCSDirectory : VCSObject
    {
        /// <summary>
        /// All subdirectories and the files directly in this directory
        /// </summary>
        internal IDictionary<string, VCSObject> Children { get; private set; }

        /// <summary>
        /// In rare cases, a path first referes to a directory and afterwards to a file. If this happens, DirectEdits
        /// is the file alias for this directory
        /// </summary>
        internal VCSFile DirectEdits { get; private set; }

        public VCSDirectory(string relativeName)
            : base(relativeName)
        {
            this.Children = new Dictionary<string, VCSObject>(10);
        }

        public override void AddReview(int idReviewer, IEnumerable<string> filenameComponents, double reviewWeight)
        {
            if (!filenameComponents.Any())  // this happens if the parent object created this object as Directory and it is afterwards used as a file
            {
                if (null == DirectEdits)
                    DirectEdits = new VCSFile(string.Empty);

                DirectEdits.AddReview(idReviewer, filenameComponents, reviewWeight);
                return;
            }
            
            string nextComponent = filenameComponents.First();
            IEnumerable<string> remainingComponents = filenameComponents.Skip(1);

            if (!Children.ContainsKey(nextComponent))
                if (remainingComponents.Any())        // it's a directory
                    Children.Add(nextComponent, new VCSDirectory(nextComponent));
                else        // it's a file
                    Children.Add(nextComponent, new VCSFile(nextComponent));

            Children[nextComponent].AddReview(idReviewer, remainingComponents, reviewWeight);
        }

        internal override void CalculateDeveloperExpertises(ConcurrentDictionary<int, double> dictExpertises, string[] filenameComponents, int currentDepth, int numberOfMatchingComponents)
        {
            int numberOfStillMatchingComponents;
            if (currentDepth - 1 == numberOfMatchingComponents && 
                filenameComponents.Length >= currentDepth &&      // this is only possible for directories that are now patched as files
                RelativeName == filenameComponents[currentDepth - 1]) // still matching
                numberOfStillMatchingComponents = numberOfMatchingComponents + 1;
            else
                numberOfStillMatchingComponents = numberOfMatchingComponents;

            Parallel.ForEach(
                Children.Values,
                childVCSObject => childVCSObject.CalculateDeveloperExpertises(dictExpertises, filenameComponents, currentDepth + 1, numberOfStillMatchingComponents)
            );

            if (null != DirectEdits)
                DirectEdits.CalculateDeveloperExpertises(dictExpertises, filenameComponents, currentDepth, numberOfMatchingComponents);
        }
    }
}
