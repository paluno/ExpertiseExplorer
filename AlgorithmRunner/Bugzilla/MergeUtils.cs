using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker;

namespace ExpertiseExplorer.AlgorithmRunner.Bugzilla
{
    /// <summary>
    /// Code from svick posted on https://stackoverflow.com/questions/7717871/how-to-perform-merge-sort-using-linq
    /// </summary>
    class MergeUtils
    {
        internal static IEnumerable<T> Merge<T>(IEnumerable<T> first,
                            IEnumerable<T> second,
                            Func<T, T, bool> predicate)
        {
            // validation ommited

            using (var firstEnumerator = first.GetEnumerator())
            using (var secondEnumerator = second.GetEnumerator())
            {
                bool firstCond = firstEnumerator.MoveNext();
                bool secondCond = secondEnumerator.MoveNext();

                while (firstCond && secondCond)
                {
                    if (predicate(firstEnumerator.Current, secondEnumerator.Current))
                    {
                        yield return firstEnumerator.Current;
                        firstCond = firstEnumerator.MoveNext();
                    }
                    else
                    {
                        yield return secondEnumerator.Current;
                        secondCond = secondEnumerator.MoveNext();
                    }
                }

                while (firstCond)
                {
                    yield return firstEnumerator.Current;
                    firstCond = firstEnumerator.MoveNext();
                }

                while (secondCond)
                {
                    yield return secondEnumerator.Current;
                    secondCond = secondEnumerator.MoveNext();
                }
            }
        }

    }
}
