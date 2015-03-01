namespace ExpertiseExplorerCommon
{
    using System;

    [Serializable]
    public class FileChange
    {
        public FileChange()
        {
        }

        public FileChange(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public bool IsNew { get; set; }

        public bool IsBinary { get; set; }

        public int LinesAdded { get; set; }

        public int LinesDeleted { get; set; }

        public string RawDiff { get; set; }
    }
}
