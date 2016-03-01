using System;
using Newtonsoft.Json;


namespace ReviewerRecommender.Models.GitLab
{
    public class MergeRequestEvent
    {
        [JsonProperty("user")]
        public GitLabUser User { get; set; }

        [JsonProperty("repository")]
        public Repository TargetRepository { get; set; }

        [JsonProperty("object_attributes")]
        public MergeRequest AffectedMergeRequest { get; set; }

        [JsonProperty("object_kind")]
        public string ObjectKind { get; set; }
    }
}