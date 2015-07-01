using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker;
using log4net;

namespace ExpertiseExplorer.AlgorithmRunner.Gerrit
{
    class GerritReviewFactory : IssueTrackerEventFactory
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public GerritReviewFactory(string GerritCSVPath)
            : base(GerritCSVPath)
        {
        }

        public override IEnumerable<IssueTrackerEvent> parseIssueTrackerEvents()
        {
            List<IssueTrackerEvent> result = new List<IssueTrackerEvent>();
            string[] lines = File.ReadAllLines(InputFilePath);

            foreach (string line in lines)
            {
                string type = line.Split(';')[1];

                if (type == "r")
                    result.Add(new GerritReview(line));
                else if (type == "c")
                    result.Add(new GerritPatchUpload(line));
                else
                    throw new SystemException("Unknown Gerrit-Type: " + type);
            }

            ISet<string> invalidChangeIds = new HashSet<string>(
                result
                    .GroupBy(ite => ite.ChangeId)
                    .Where(oneChange => MinBy(oneChange, ite => ite.When) is GerritReview)   // in which changes is the first event a review instead of a commit?
                    .Select(oneChange => oneChange.Key)                                 // remember these changes as invalid
            );

            if (invalidChangeIds.Any())
            {
                Log.Warn("Skipping " + invalidChangeIds.Count + " changes, because they had a review before the first commit: " +
                    string.Join(", ", invalidChangeIds)
                    );
                return result.Where(ite => !invalidChangeIds.Contains(ite.ChangeId));
            }
            else
                return result;
        }

        public static TSource MinBy<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> selector)
        {
            Comparer<TKey> cmp = Comparer<TKey>.Default;
            return source.Aggregate((curMin, x) => cmp.Compare(selector(x),selector(curMin))<0 ? x : curMin);
        }
    }
}
