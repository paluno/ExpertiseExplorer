﻿using Newtonsoft.Json;
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

        //public async Task<List<Commit>> FetchCommitsAsync()
        //{
        //     HttpResponseMessage response = await GitLabAPI.GetAsync(
        //        string.Format("projects/{0}/merge_requests/{1}/commits", TargetProjectId, Id)
        //        );

        //    response.EnsureSuccessStatusCode();

        //    return await response.Content.ReadAsAsync<List<Commit>>();
        //}

        public async Task FetchChangesAsync()
        {
            if (this.Changes != null)
                return;     // this MergeRequest already has the list of changes

            HttpResponseMessage response = await GitLabAPI.GetAsync(
               string.Format("projects/{0}/merge_requests/{1}/changes", TargetProjectId, Id)
               );

            response.EnsureSuccessStatusCode();

            MergeRequest other = await response.Content.ReadAsAsync<MergeRequest>();
            this.Changes = other.Changes;
                // Update some other info that may be newer in the other merge request
            if (this.UpdatedAt == DateTime.MinValue)
                this.UpdatedAt = other.UpdatedAt;
        }

        public async Task<IEnumerable<string>> FetchFilesAffectedByMergeRequest()
        {
            await FetchChangesAsync();
            return Changes.Select(c => c.NewPath);
        }

    }
}