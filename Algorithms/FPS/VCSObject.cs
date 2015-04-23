using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms.FPS
{
    abstract class VCSObject
    {
        public string RelativeName { get; private set; }

        public VCSObject(string name)
        {
            this.RelativeName = name;
        }

        public abstract void AddReview(string reviewer, IEnumerable<string> filenameComponents, double reviewWeight);

        /// <summary>
        /// Helps calculating the FPS score, but should not be called from other classes than FPSObjects (cannot make it
        /// protected, though, as derived classes couldn't call it anymore on other objects in C#).
        /// </summary>
        /// <param name="dictExpertises">VCSFiles add results to this dictionary, VCSDirectories just pass it to their children</param>
        /// <param name="filenameComponents">The components of the checked filename</param>
        /// <param name="currentDepth">Which depth of the tree are we currently in, i.e. what is the depth of the called object?</param>
        /// <param name="numberOfMatchingComponents">When comparing the current object's filename with the checked filename, on how many
        ///     componentens do they match? Every callee has to evaluate current matching until after the first difference.</param>
        internal abstract void CalculateDeveloperExpertises(ConcurrentDictionary<string, double> dictExpertises, string[] filenameComponents, int currentDepth, int numberOfMatchingComponents);
     }
}
