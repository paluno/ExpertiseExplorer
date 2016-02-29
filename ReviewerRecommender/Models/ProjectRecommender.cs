using ExpertiseExplorer.Algorithms;
using ReviewerRecommender.Models.GitLab;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace ReviewerRecommender.Models
{
    public class ProjectRecommender
    {
        public string ProjectName { get; }

        //        public WeightedReviewCountAlgorithm recommendationAlgorithm { get; set; }

        private static ConcurrentDictionary<string, ProjectRecommender> dictRecommenders { get; } 
            = new ConcurrentDictionary<string, ProjectRecommender>();


        public static ProjectRecommender FindProjectRecommender(string projectName)
        {
            if (dictRecommenders.ContainsKey(projectName))
                return dictRecommenders[projectName];
            else
                lock(dictRecommenders)
                    if (dictRecommenders.ContainsKey(projectName))
                        return dictRecommenders[projectName];
                    else
                        return dictRecommenders[projectName] = new ProjectRecommender(projectName);
        }

        private ProjectRecommender(string projectName)
        {
            this.ProjectName = projectName;
        }

        public void orderReviewerRecommendation(MergeRequest mr)
        {
            ThreadPool.QueueUserWorkItem(delegate(object dummy) { recommendReviewersAsync(mr).Wait(); });
        }

        protected async Task recommendReviewersAsync(MergeRequest mr)
        {
            List<Commit> commits = await mr.FetchCommitsAsync();
            IEnumerable<string> allFiles =
                commits
                    .Select(commit => commit.Added.Concat(commit.Modified).Concat(commit.Removed))
                    .Aggregate((c1, c2) => c1.Union(c2));

            // Todo: Calculate good reviewers, post to MR
            //Algorithm.GetDevelopersForArtifactsAsync()
        }
    }
}