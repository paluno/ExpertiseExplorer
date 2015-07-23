using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace ExpertiseExplorer.AlgorithmRunner.AbstractIssueTracker
{
    /// <summary>
    /// Abstract Factory
    /// </summary>
    abstract class IssueTrackerEventFactory
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public string InputFilePath { get; set; }

        abstract public IEnumerable<IssueTrackerEvent> parseIssueTrackerEvents();

        abstract protected IEnumerable<IssueTrackerEvent> PrefilterRawInput(string pathToRawInputFile);


        protected IssueTrackerEventFactory(string inputFilePath)
        {
            this.InputFilePath = inputFilePath;
        }

        /// <summary>
        /// parses and filters a raw input file and writes back a chronologically ordered file
        /// </summary>
        public void PrepareInput(string pathToRawInputFile, bool overwrite = false)
        {
            if (!overwrite && File.Exists(InputFilePath))
                return;

            IEnumerable<IssueTrackerEvent> list = PrefilterRawInput(pathToRawInputFile);

            // ordering of & another filter pass on the reviews
            IDictionary<DateTime, ICollection<IssueTrackerEvent>> dictIssueTrackerEvents = new Dictionary<DateTime, ICollection<IssueTrackerEvent>>(20000);
            foreach (IssueTrackerEvent currentEvent in list)
            {
                if (!currentEvent.isValid())
                {
                    Log.Warn("Skipping invalid event " + currentEvent);
                    continue;
                }

                if (!dictIssueTrackerEvents.ContainsKey(currentEvent.When))
                    dictIssueTrackerEvents.Add(currentEvent.When, new LinkedList<IssueTrackerEvent>());
                dictIssueTrackerEvents[currentEvent.When].Add(currentEvent);
            }

            // list is ordered by whatever, maybe ChangeId, but not datetime
            var sb = new StringBuilder();
            foreach (ICollection<IssueTrackerEvent> nextEventCollection in dictIssueTrackerEvents.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value))
                foreach (IssueTrackerEvent ite in nextEventCollection)
                    sb.AppendLine(ite.ToString());

            Debug.WriteLine("Finished ordering at: " + DateTime.Now);

            File.WriteAllText(InputFilePath, sb.ToString());
        }
    }
}
