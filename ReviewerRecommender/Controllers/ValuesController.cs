using ExpertiseExplorer.Algorithms;
using ExpertiseExplorer.Algorithms.FPS;
using ExpertiseExplorer.Algorithms.RepositoryManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace ReviewerRecommender.Controllers
{
    public class ValuesController : ApiController
    {
        public static WeightedReviewCountAlgorithm reviewAlgorithm;

        static ValuesController()
        {
            RootDirectory fpsTree = new RootDirectory();
            reviewAlgorithm = new WeightedReviewCountAlgorithm(fpsTree);
            reviewAlgorithm.LoadReviewScoresFromDB();

            reviewAlgorithm.Deduplicator = new AliasFinder();

            SourceRepositoryConnector SourceManager = new SourceRepositoryConnector();
            SourceManager.InitIdsFromDbForSourceUrl("test", false);

            reviewAlgorithm.SourceRepositoryManager = SourceManager;
            reviewAlgorithm.RepositoryId = SourceManager.RepositoryId;

        }

        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        public void review(string reviewer, string[] files, DateTime reviewTime)
        {
            reviewAlgorithm.AddReviewScore(reviewer, files, reviewTime);
        }

        public void getReviewTest(string reviewer, DateTime reviewTime)
        {
            reviewAlgorithm.AddReviewScore(reviewer, new string[] { "file/A", "file/B" }, reviewTime);
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }
    }
}
