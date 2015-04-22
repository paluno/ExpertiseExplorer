using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms.FPS
{
    class TopLevelDirectory : VCSDirectory
    {
        public ISet<string> Developers { get; private set; }

        internal TopLevelDirectory(string relativeName)
            : base(relativeName)
        {
            Developers = new HashSet<string>();
        }

        public override void AddReview(string reviewer, IEnumerable<string> filenameComponents, double reviewWeight)
        {
            base.AddReview(reviewer, filenameComponents, reviewWeight);

            Developers.Add(reviewer);
        }
    }
}
