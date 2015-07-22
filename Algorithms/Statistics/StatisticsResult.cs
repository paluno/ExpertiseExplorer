using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ExpertiseExplorer.Algorithms.Statistics
{
    internal class StatisticsResult
    {
        public const int NUMBER_OF_EXPERTS = 5; // because it's five computed reviewers. 

        public StatisticsResult(int bugId)
        {
            BugId = bugId;
            Matches = new bool[NUMBER_OF_EXPERTS];  
        }

        /// <summary>
        /// Which of the five computed experts were correct predictions?
        /// </summary>
        public bool[] Matches { get; private set;  }

        public int BugId { get; private set; }

        public bool IsMatch { get { return Matches.Any(entry => entry); } }

        public static StatisticsResult FromCSVLine(string csvline)
        {
            string[] tmp = csvline.Split(';');
            Debug.Assert(2 == tmp.Length);

            int bugId = int.Parse(tmp[0]);
            IEnumerable<int> matchedNumbers = tmp[1]
                .Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(matchString => int.Parse(matchString));

            StatisticsResult sr = new StatisticsResult(bugId);
            foreach (int i in Enumerable.Range(0, NUMBER_OF_EXPERTS))
                sr.Matches[i] = matchedNumbers.Contains(i);
            return sr;
        }

        public string ToCSV()
        {
            return BugId + ";" + string.Join(",", Enumerable.Range(0, NUMBER_OF_EXPERTS).Where(num => Matches[num]));
        }
    }
}