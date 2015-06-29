using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms.FPS
{
    public class RootDirectory : VCSDirectory
    {
        public RootDirectory()
            : base("/")
        {

        }

        /// <summary>
        /// Increases the reviewer's expertise on the reviewed files for making a review.
        /// </summary>
        public void AddReview(string reviewer, IEnumerable<string> reviewedFiles)
        {
            double reviewWeight = 1D / reviewedFiles.Count();
            foreach (string filename in reviewedFiles)
                AddReview(reviewer, filename.Split('/'), reviewWeight);
        }

        /// <summary>
        /// Calculates which developers have expertise with the given file and for each developer the specific FPS score.
        /// </summary>
        /// <param name="filename">The expertise for this file is to be calculated.</param>
        /// <returns>The dictionary's keys are developer names. The values are their FPS scores.</returns>
        public IDictionary<string, double> CalculateDeveloperExpertisesForFile(string filename)
        {
            string[] filenameComponents = filename.Split('/');
            ConcurrentDictionary<string, double> dictExpertises = new ConcurrentDictionary<string, double>();
            CalculateDeveloperExpertises(dictExpertises, filenameComponents, 0, 0);
            return dictExpertises;
        }

        internal override void CalculateDeveloperExpertises(ConcurrentDictionary<string, double> dictExpertises, string[] filenameComponents, int currentDepth, int numberOfMatchingComponents)
        {
            if (!Children.ContainsKey(filenameComponents[0]))
                return;    // this is in a new directory or a new file. Nobody knows anything.

            VCSObject suitableChild = Children[filenameComponents[0]];  // All others have a FileSimilarity of 0 and can be disregarded immediately.
            suitableChild.CalculateDeveloperExpertises(dictExpertises, filenameComponents, 1, 0);
        }
    }
}
