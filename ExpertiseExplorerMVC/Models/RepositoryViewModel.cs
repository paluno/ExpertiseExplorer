namespace ExpertiseExplorerMVC.Models
{
    using System;
    using System.Collections.Generic;

    public class RepositoryViewModel
    {
        public string Name { get; set; }

        public int Id { get; set; }

        public List<Tuple<string, int, double>> Developers { get; set; }
    }
}