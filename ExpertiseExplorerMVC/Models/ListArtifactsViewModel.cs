namespace ExpertiseExplorer.MVC.Models
{
    using System;
    using System.Collections.Generic;

    public class ListArtifactsViewModel
    {
        public int RepositoryId { get; set; }

        public string Name { get; set; }

        public string ActiveChar { get; set; }

        public List<Tuple<string, int>> Artifacts { get; set; } 
    }
}