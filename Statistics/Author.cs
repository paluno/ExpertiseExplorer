namespace Statistics
{
    using System.Collections.Generic;
    using System.Linq;

    internal class Author
    {
        public Author()
        {
            Alternatives = new List<Author>();
        }

        public string Name { get; set; }

        public List<Author> Alternatives { get; set; }

        public bool IsNameInAlternatives(string name)
        {
            return Alternatives.Any(a => a.Name.ToLowerInvariant().Contains(name.ToLowerInvariant()));
        }

        public bool IsNameInAlternativesOrSelf(string name)
        {
            return Name.ToLowerInvariant().Contains(name.ToLowerInvariant()) || IsNameInAlternatives(name);
        }

        public bool IsMatching(Author other)
        {
            return IsNameInAlternativesOrSelf(other.Name) || other.Alternatives.Any(alternative => IsNameInAlternativesOrSelf(alternative.Name));
        }
    }
}