using ExpertiseExplorer.Algorithms;
using ReviewerRecommender.Models;
using ReviewerRecommender.Models.GitLab;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace ReviewerRecommender.Controllers
{
    [RoutePrefix("api/Projects/{project}")]
    public class ProjectsController : ApiController
    {


        [Route("merge")]
        [HttpPost]
        public void PostMerge(string project, MergeRequestEvent mr)
        {
            if (mr.ObjectKind != "merge_request")
                BadRequest("This URI is only for events of object_kind merge_request, but the posted object is different.");
            else
            {
                if (mr.AffectedMergeRequest.Action == "close")  // TODO: or merge
                {
                    // Update reviewer expertise
                    ProjectRecommender.FindProjectRecommender(project).orderExpertiseAward(mr.User, mr.AffectedMergeRequest);
                }
                else if (mr.AffectedMergeRequest.Action == "new")
                {
                    // recommend reviewers
                    ProjectRecommender.FindProjectRecommender(project).orderReviewerRecommendation(mr.AffectedMergeRequest);
                }

                Ok();
            }
        }
    }
}
