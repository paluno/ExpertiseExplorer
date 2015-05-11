namespace Algorithms.Statistics
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    internal class Author
    {
        public Author(string name)
        {
            Alternatives = new List<Author>();
            this.completeName = name;
        }

        public readonly string completeName;

        private bool fPartParsed = false;
        private string _mailPart;
        private string _loginNamePart;
        private string _namePart;

        public string MailPart
        {
            get
            {
                ParseParts();
                return _mailPart;
            }
        }

        public string LoginNamePart
        {
            get
            {
                ParseParts();
                return _loginNamePart;
            }
        }

        public string NamePart
        {
            get
            {
                ParseParts();
                return _namePart;
            }
        }

        private static readonly Regex rxFindParts = new Regex(@"^(?<NamePart>[\w -]+)" +
            @"(?: (?:\[|\():?(?<LoginNamePart>\w+)(?:\)|\]))?" +
            @"(?: <(?<MailPart>[^>]+)>)?$", RegexOptions.Compiled);
        
        /// <summary>
        /// Idempotent method to parse the three parts NamePart, LoginNamePart, and MailPart in a CompleteName
        /// </summary>
        private void ParseParts()
        {
            if (fPartParsed)
                return;
            fPartParsed = true;

            Match mtParts = rxFindParts.Match(completeName);
            if (mtParts.Success)
            {
                if (mtParts.Groups["NamePart"].Success)
                    _namePart = mtParts.Groups["NamePart"].Value;
                if (mtParts.Groups["MailPart"].Success)
                    _mailPart = mtParts.Groups["MailPart"].Value;
                if (mtParts.Groups["LoginNamePart"].Success)
                    _loginNamePart = mtParts.Groups["LoginNamePart"].Value;
            }
            else if (completeName.Contains("@") && !completeName.Contains(" "))
                _mailPart = completeName.Trim('<', '>');
            else
                _namePart = completeName;
        }

        public List<Author> Alternatives { get; set; }

        public bool IsNameInAlternatives(string name)
        {
            return Alternatives.Any(a => a.completeName.ToLowerInvariant().Contains(name.ToLowerInvariant()));
        }

        public bool IsNameInAlternativesOrSelf(string name)
        {
            return completeName.ToLowerInvariant().Contains(name.ToLowerInvariant()) || IsNameInAlternatives(name);
        }

        public bool IsMatching(Author other)
        {
            return IsNameInAlternativesOrSelf(other.completeName) || other.Alternatives.Any(alternative => IsNameInAlternativesOrSelf(alternative.completeName));
        }

        public void mergeOtherAuthors(IEnumerable<Author> others)
        {
            Alternatives.AddRange(others);
            foreach (Author other in others)
                other.Alternatives.Add(this);
        }

        public static List<Author> GetAuthorsFromFile(string file)
        {
            string[] authorLines = File.ReadAllLines(file);

            List<Author> result = new List<Author>();

            foreach (string authorLine in authorLines)
                result.AddRange(GetAuthorsFromLine(authorLine));

            return result;
        }

        public static List<Author> GetAuthorsFromLine(string line)
        {
            return MergeAliasesIntoOneAuthor(line.Split(';', ','));
        }

        private static List<Author> MergeAliasesIntoOneAuthor(IEnumerable<string> names)
        {
            List<Author> result = names.Select(name => new Author(name)).ToList();

            for (int i = 0; i < result.Count; i++)
                result[i].mergeOtherAuthors(result.Skip(i+1));

            return result;
        }
    }
}