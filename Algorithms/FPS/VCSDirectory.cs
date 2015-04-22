using System;
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

        /// <summary>
        /// This method assumes that the subdirectory doesn't exist yet. This has to be checked in advance. It then creates the subdirectory.
        /// </summary>
        protected virtual void AddSubDirectory(string directoryName)
        {
            Children.Add(directoryName, new VCSDirectory(directoryName));
        }

        public override void AddReview(string reviewer, IEnumerable<string> filenameComponents, double reviewWeight)
        {
            string nextComponent = filenameComponents.First();
            IEnumerable<string> remainingComponents = filenameComponents.Skip(1);

            if (!Children.ContainsKey(nextComponent))
                if (remainingComponents.Count() > 1)        // it's a directory
                    AddSubDirectory(nextComponent);
                else        // it's a file
                    Children.Add(nextComponent, new VCSFile(nextComponent));

            Children[nextComponent].AddReview(reviewer, remainingComponents, reviewWeight);
        }
    }
}
