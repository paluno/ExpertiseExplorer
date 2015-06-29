using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpertiseExplorer.ExpertiseDB.Extensions;

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
        public void AddReview(int idReviewer, IEnumerable<string> reviewedFiles)
        {
            double reviewWeight = 1D / reviewedFiles.Count();
            foreach (string filename in reviewedFiles)
                AddReview(idReviewer, filename.Split('/'), reviewWeight);
        }

        /// <summary>
        /// Calculates which developers have expertise with the given file and for each developer the specific FPS score.
        /// </summary>
        /// <param name="filename">The expertise for this file is to be calculated.</param>
        /// <returns>The dictionary's keys are developer database IDs. The values are their FPS scores.</returns>
        public IEnumerable<DeveloperWithExpertise> CalculateDeveloperExpertisesForFile(string filename)
        {
            string[] filenameComponents = filename.Split('/');
            ConcurrentDictionary<int, double> dictExpertises = new ConcurrentDictionary<int, double>();
            CalculateDeveloperExpertises(dictExpertises, filenameComponents, 0, 0);
            return dictExpertises
                .Select(kvp => new DeveloperWithExpertise(kvp.Key, kvp.Value));
        }

        internal override void CalculateDeveloperExpertises(ConcurrentDictionary<int, double> dictExpertises, string[] filenameComponents, int currentDepth, int numberOfMatchingComponents)
        {
            if (!Children.ContainsKey(filenameComponents[0]))
                return;    // this is in a new directory or a new file. Nobody knows anything.

            VCSObject suitableChild = Children[filenameComponents[0]];  // All others have a FileSimilarity of 0 and can be disregarded immediately.
            suitableChild.CalculateDeveloperExpertises(dictExpertises, filenameComponents, 1, 0);
        }
    }
}
