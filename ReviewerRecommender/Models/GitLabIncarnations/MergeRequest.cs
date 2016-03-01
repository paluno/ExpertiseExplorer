using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace ReviewerRecommender.Models.GitLab
{
    public partial class MergeRequest
    {
        public enum MergeRequestAction { open, reopen, close, merge }

        [JsonProperty("action")]
        public MergeRequestAction Action { get; set; }

        public HttpClient GitLabAPI { get; set; }

        public MergeRequest()
        {
            GitLabAPI = new HttpClient();
            
            GitLabAPI.BaseAddress = new Uri(ConfigurationManager.AppSettings["GitLabAPIURL"]);
            GitLabAPI.DefaultRequestHeaders.Add("PRIVATE-TOKEN", ConfigurationManager.AppSettings["GitLabAPIKey"]);
        }

        public async Task<List<Commit>> FetchCommitsAsync()
        {
             HttpResponseMessage response = await GitLabAPI.GetAsync(
                string.Format("/projects/{0}/merge_requests/{1}/commits", TargetProjectId, Id)
                );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<List<Commit>>();
        }

        public async Task<IEnumerable<string>> FetchFilesAffectedByMergeRequest()
        {
            List<Commit> commits = await FetchCommitsAsync();
            return
                commits
                    .Select(commit => commit.Added
                                        .Concat(commit.Modified)
                                        .Concat(commit.Removed)
                           )
                    .Aggregate((c1, c2) => c1.Union(c2));
        }

    }
}