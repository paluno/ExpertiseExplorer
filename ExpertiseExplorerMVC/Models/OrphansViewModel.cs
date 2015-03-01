namespace ExpertiseExplorerMVC.Models
{
    using System;
    using System.Collections.Generic;

    public class OrphansViewModel
    {
        public int RepositoryId { get; set; }

        public string RepositoryName { get; set; }

        public List<Tuple<string, int>> PotentialOrphans { get; set; } 
    }
}