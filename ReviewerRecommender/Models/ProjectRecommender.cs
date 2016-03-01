using ExpertiseExplorer.Algorithms;
using ExpertiseExplorer.ExpertiseDB;
using ReviewerRecommender.Models.GitLab;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Data.Entity;
using ExpertiseExplorer.Algorithms.FPS;
using ExpertiseExplorer.Algorithms.RepositoryManagement;

namespace ReviewerRecommender.Models
{
    public class ProjectRecommender
    {
        public string ProjectName { get; }

        public int RepositoryId { get; }

        public WeightedReviewCountAlgorithm recommendationAlgorithm { get; set; }

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

            RootDirectory fpsTree = new RootDirectory();
            recommendationAlgorithm = new WeightedReviewCountAlgorithm(fpsTree);

            SourceRepositoryConnector SourceManager = new SourceRepositoryConnector();
            SourceManager.Deduplicator = new AliasFinder();
            SourceManager.EnsureSourceRepositoryExistance(projectName);
            SourceManager.InitIdsFromDbForSourceUrl(projectName, false);
            recommendationAlgorithm.Deduplicator = SourceManager.Deduplicator;
            recommendationAlgorithm.RepositoryId = SourceManager.RepositoryId;
            this.RepositoryId = SourceManager.RepositoryId;
        }

        public void orderReviewerRecommendation(MergeRequest mr)
        {
            ThreadPool.QueueUserWorkItem(delegate(object dummy) { recommendReviewersAsync(mr).Wait(); });
        }

        public void orderExpertiseAward(GitLabUser initiator, MergeRequest mr)
        {
            ThreadPool.QueueUserWorkItem(delegate (object dummy) { awardReviewerExpertise(initiator, mr).Wait(); });
        }

        protected async Task recommendReviewersAsync(MergeRequest mr)
        {
            IEnumerable<string> affectedFiles = await mr.FetchFilesAffectedByMergeRequest();

            // Todo: Calculate good reviewers, post to MR
            //Algorithm.GetDevelopersForArtifactsAsync()
        }

        protected object bugCreationLock = new object();
        protected async Task<Bug> findOrCreateBug(ExpertiseDBEntities db, string changeId)
        {
            Bug foundBug = await db.Bugs.SingleOrDefaultAsync(b => b.RepositoryId == RepositoryId && b.ChangeId == changeId);
            if (null == foundBug)
            {
                lock (bugCreationLock)
                {
                    using (var freshDb = new ExpertiseDBEntities())
                        foundBug = freshDb.Bugs.SingleOrDefault(b => b.RepositoryId == RepositoryId && b.ChangeId == changeId);
                    if (null == foundBug)
                    {
                        foundBug = db.Bugs.Add(new Bug()
                        {
                            RepositoryId = this.RepositoryId,
                            ChangeId = changeId
                        });
                        db.SaveChanges();
                    }
                    else
                        foundBug = db.Bugs.SingleOrDefault(b => b.RepositoryId == RepositoryId && b.ChangeId == changeId);  // re-retrieve from original context for further use
                }
            }
            return foundBug;
        }

        protected object reviewerCreationLock = new object();

        protected async Task awardReviewerExpertise(GitLabUser user, MergeRequest mr)
        {
            using (var db = new ExpertiseDBEntities())
            {
                    // 1. Check whether the user has already been awarded review expertise for this MergeRequest
                Bug b = await findOrCreateBug(db, mr.Iid.ToString());
                if (b.ActualReviewers.Any(ar => ar.Reviewer == user.Username))
                    return;
                lock (reviewerCreationLock)
                {
                    using (var freshDb = new ExpertiseDBEntities())
                        if (freshDb.ActualReviewers.Any(ar => ar.BugId == b.BugId && ar.Reviewer == user.Username))
                        //{
                        //    var x = freshDb.ActualReviewers.Where(ar => ar.BugId == b.BugId && ar.Reviewer == user.Username);
                        //    int z = x.Count();
                        //    var y = x.FirstOrDefault();
                            return;
                        //}

                    b.ActualReviewers.Add(new ActualReviewer()
                    {
                        Reviewer = user.Username,
                        ActivityId = mr.UpdatedAt.GetHashCode()
                    });
                    db.SaveChanges();
                }

                    // 2. Find the files affected by the MergeRequest
                IEnumerable<string> affectedFiles = await mr.FetchFilesAffectedByMergeRequest();

                    // 3. Award expertise for the affected files
                recommendationAlgorithm.AddReviewScore(user.Email, affectedFiles.ToList(), mr.UpdatedAt);
            }
        }
    }
}