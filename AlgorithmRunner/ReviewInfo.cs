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

        public virtual int ActivityId { get; set; }

        public virtual string Reviewer { get; set; }

        public virtual DateTime When { get; set; }

        public virtual IList<String> Filenames { get; set; }

        public abstract bool isValid();

        public override string ToString()
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var originalUtc = When.Add(new TimeSpan(0, 7, 0, 0));
            var secondspassed = Convert.ToInt64((originalUtc - epoch).TotalSeconds);
            return ChangeId + ";" + ActivityId + ";" + Reviewer + ";" + secondspassed;
        }
    }
}
