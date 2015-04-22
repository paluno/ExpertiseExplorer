using System;
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
     }
}
