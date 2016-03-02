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

        protected WeightedReviewCountAlgorithm recommendationAlgorithm { get; }

        protected SourceRepositoryConnector SourceManager { get; }

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
                    {
                        ProjectRecommender pr = new ProjectRecommender(projectName);
                        pr.initRecommendationAlgorithm();
                        return dictRecommenders[projectName] = pr;
                    }
        }

        private ProjectRecommender(string projectName)
        {
            this.ProjectName = projectName;

            RootDirectory fpsTree = new RootDirectory();
            recommendationAlgorithm = new WeightedReviewCountAlgorithm(fpsTree);

            SourceManager = new SourceRepositoryConnector();
            SourceManager.Deduplicator = new AliasFinder();
            SourceManager.EnsureSourceRepositoryExistance(ProjectName);
            SourceManager.InitIdsFromDbForSourceUrl(ProjectName, false);
            recommendationAlgorithm.SourceRepositoryManager = SourceManager;
            recommendationAlgorithm.Deduplicator = SourceManager.Deduplicator;
            recommendationAlgorithm.RepositoryId = SourceManager.RepositoryId;
            this.RepositoryId = SourceManager.RepositoryId;
        }

        protected void initRecommendationAlgorithm()
        {
            recommendationAlgorithm.LoadReviewScoresFromDB();
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

            IEnumerable<int> artifactIds = affectedFiles
                .Select(fileName => SourceManager.FindOrCreateFileArtifactId(fileName));
            ComputedReviewer cr = await recommendationAlgorithm.GetDevelopersForArtifactsAsync(artifactIds);

            string[] recommendedReviewers = new string[5];  // Magic number: we always recommend up to 5 reviewers.
            using (var db = new ExpertiseDBEntities())
            {
                Task<Developer>[] taskFindDevelopers = new Task<Developer>[recommendedReviewers.Length];
                if (null != cr.Expert1Id)
                    taskFindDevelopers[0] = db.Developers.FindAsync(cr.Expert1Id);
                if (null != cr.Expert2Id)
                    taskFindDevelopers[1] = db.Developers.FindAsync(cr.Expert2Id);
                if (null != cr.Expert3Id)
                    taskFindDevelopers[2] = db.Developers.FindAsync(cr.Expert3Id);
                if (null != cr.Expert4Id)
                    taskFindDevelopers[3] = db.Developers.FindAsync(cr.Expert4Id);
                if (null != cr.Expert5Id)
                    taskFindDevelopers[4] = db.Developers.FindAsync(cr.Expert5Id);


            }
            // Todo: Calculate good reviewers, post to MR
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
                recommendationAlgorithm.AddReviewScore(user.Username, affectedFiles.ToList(), mr.UpdatedAt);
            }
        }
    }
}