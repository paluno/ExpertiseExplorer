using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExpertiseExplorer.Algorithms.Statistics
{
    public abstract class SourceOfActualReviewers
    {
        public enum StatisticsSource
        {
            All = 0,
            WithoutHg = 1
        }

        public int RepositoryId { get; private set; }

        #region construction
        public SourceOfActualReviewers(int repositoryId)
        {
            this.RepositoryId = repositoryId;
        }

        /// <summary>
        /// Factory Method for Sources
        /// </summary>
       public static SourceOfActualReviewers createSourceFromParameter(StatisticsSource typeOfSource, int repositoryId)
        {
            switch(typeOfSource)
            {
                case StatisticsSource.All:
                    return new SourceOfAllActualReviewers(repositoryId);
                case StatisticsSource.WithoutHg:
                    return new SourceOfActualReviewersWithoutHg(repositoryId);
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

        public IEnumerable<int> findReviews()
        {
            if (null == reviewCache)
                lock (lock4Reviewcache)
                    if (null == reviewCache)
                        reviewCache = findReviewsInDatabase();

            return reviewCache;
        }

        protected abstract IEnumerable<int> findReviewsInDatabase();
        public abstract IDictionary<int, string> findReviewsWithReviewers();
    }
}
