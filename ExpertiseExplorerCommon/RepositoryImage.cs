namespace ExpertiseExplorerCommon
{
    using System;

    [Serializable]
    public class RepositoryImage
    {
        public RepositoryImage()
        {
        }

        public RepositoryImage(string source)
        {
            Source = source;
        }

        public Changeset[] Changesets { get; set; }

        public string Source { get; set; }
    }
}
