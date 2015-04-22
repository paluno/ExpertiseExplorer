using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms.FPS
{
    class RootDirectory : VCSDirectory
    {
        public RootDirectory()
            : base("/")
        {

        }

        public TopLevelDirectory GetTopDirectory(string name)
        {
            return (TopLevelDirectory)Children[name];
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

        protected override void AddSubDirectory(string directoryName)
        {
            Children.Add(directoryName, new TopLevelDirectory(directoryName));
        }
    }
}
