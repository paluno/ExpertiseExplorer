using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms.Statistics
{
    public abstract class AbstractSourceOfBugs
    {
        public enum StatisticsSource
        {
            All = 0,
            WithoutHg = 1,
            MinimumId = 2
        }

        public int RepositoryId { get; private set; }

        #region construction
        public AbstractSourceOfBugs(int repositoryId)
        {
            this.RepositoryId = repositoryId;
        }

        /// <summary>
        /// Factory Method for Sources
        /// </summary>
       public static AbstractSourceOfBugs createSourceFromParameter(StatisticsSource typeOfSource, int repositoryId, string[] additionalParameters)
        {
            switch(typeOfSource)
            {
                case StatisticsSource.All:
                    if (additionalParameters != null && additionalParameters.Length > 0)
                        throw new ArgumentException("No additional parameters expected, but those were present", "additionalParameters");
                    return new SourceOfAllBugs(repositoryId);
                case StatisticsSource.WithoutHg:
                    if (additionalParameters != null && additionalParameters.Length > 0)
                        throw new ArgumentException("No additional parameters expected, but those were present", "additionalParameters");
                    return new SourceOfBugsWithoutSpecificReviewer(repositoryId);
                case StatisticsSource.MinimumId:
                    if (additionalParameters.Length != 1)
                        throw new ArgumentException("Exactly one additional parameters expected, but this was not the case", "additionalParameters");
                    return new SourceOfBugsWithMinID(repositoryId, int.Parse(additionalParameters[0]));
                default:
                    throw new NotImplementedException("The type \"" + typeOfSource + "\" is unknown");
            }
        }
        #endregion construction

        public abstract string Postfix
        {
            get;
        }

        private IEnumerable<int> reviewCache;
        private object lock4Reviewcache = new object();

        public IEnumerable<int> findBugs()
        {
            if (null == reviewCache)
                lock (lock4Reviewcache)
                    if (null == reviewCache)
                        reviewCache = findBugsInDatabase();

            return reviewCache;
        }

        protected abstract IEnumerable<int> findBugsInDatabase();
    }
}
