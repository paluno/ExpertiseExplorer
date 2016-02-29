using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace ReviewerRecommender.Models.GitLab
{
    public partial class MergeRequest
    {
        public HttpClient GitLabAPI { get; set; }

        public MergeRequest()
        {
            GitLabAPI = new HttpClient();
            GitLabAPI.BaseAddress = new Uri("http://192.168.56.101/api/v3/");
            GitLabAPI.DefaultRequestHeaders.Add("PRIVATE-TOKEN", "9koXpg98eAheJpvBs5tK");
        }

        public async Task<List<Commit>> FetchCommitsAsync()
        {
             HttpResponseMessage response = await GitLabAPI.GetAsync(
                string.Format("/projects/{0}/merge_requests/{1}/commits", ProjectId, Id)
                );

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsAsync<List<Commit>>();
        }
    }
}