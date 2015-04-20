using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlgorithmRunner
{
    /// <summary>
    /// Abstract Factory
    /// </summary>
    abstract class ReviewInfoFactory
    {
        abstract public IEnumerable<ReviewInfo> parseReviewInfos();
    }
}
