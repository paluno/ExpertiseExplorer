using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner
{
    abstract class ReviewInfo
    {
        public virtual string ChangeId { get; set; }

        public abstract int ActivityId { get; set; }

        public virtual string Author { get; set; }

        public virtual DateTime When { get; set; }

        public virtual IList<String> Filenames { get; set; }

    }
}
