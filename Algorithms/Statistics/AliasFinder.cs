using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms.Statistics
{
    public class AliasFinder
    {
        private IDictionary<string, ISet<string>> mailaddress2name = new Dictionary<string, ISet<string>>();
        private IDictionary<string, ISet<string>> name2mailaddress = new Dictionary<string, ISet<string>>();
        private IDictionary<string, ISet<string>> mail2mail = new Dictionary<string, ISet<string>>(); // may contain an eigenreference, too
        private IDictionary<string, ISet<string>> name2name = new Dictionary<string, ISet<string>>(); // may contain an eigenreference, too

        /// <summary>
        /// Adds a new value to the dictionary that maps keys to multiple values.
        /// </summary>
        /// <returns>True if the key existed already and the value was new, otherwise (key did not exist or (key and value existed)) false</returns>
        public static bool Add2CollectionDictionary(IDictionary<string, ISet<string>> dictionary, string key2add, string value2add)
        {
            if (dictionary.ContainsKey(key2add))
                return dictionary[key2add].Add(value2add);
            else
            {
                dictionary.Add(key2add, new HashSet<string>(new string[] { value2add }));
                return false;
            }
        }
        
        public AliasFinder()
        {
        }

        public void InitializeMappingFromNames(IEnumerable<string> nameMappings)
        {
            foreach (string line in nameMappings)
            {
                string[] lineComponents = line.Split(';');
                switch (lineComponents[0])
                {
                    case "u2n":
                        {
                            if (4 == lineComponents.Length)
                                continue;   // some rows do not contain a name. No idea what they are for.
                            Debug.Assert(5 == lineComponents.Length);

                            string mailAddress = lineComponents[1];
                            // in rare cases, it is no email address, but who cares?
                            // Debug.Assert(mailAddress.Contains("@"), mailAddress + " is not a valid mail address");

                            string[] nameWithStuff = lineComponents[4].Split('=');
                            Debug.Assert(2 == nameWithStuff.Length);
                            string name = nameWithStuff[0];

                            LinkMailAddressAndNameWithConsistency(mailAddress, name);
                        }
                        break;
                    case "n2u":
                        {
                            Debug.Assert(5 <= lineComponents.Length);

                            string name = lineComponents[1];

                            for (int i = 4; i < lineComponents.Length; ++i)
                            {
                                string[] mailAddressWithStuff = lineComponents[i].Split('=');
                                Debug.Assert(2 == mailAddressWithStuff.Length);
                                string mailAddress = mailAddressWithStuff[0];
                                // Debug.Assert(mailAddress.Contains("@"), mailAddress + " is not a valid mail address");

                                LinkMailAddressAndNameWithConsistency(mailAddress, name);
                            }
                        }
                        break;
                    case "m2m":
                        Debug.Assert(3 == lineComponents.Length);
                        //Debug.Assert(lineComponents[1].Contains("@"), lineComponents[1] + " is not a valid mail address");
                        //Debug.Assert(lineComponents[2].Contains("@"), lineComponents[2] + " is not a valid mail address");

                        LinkMailAddressWithMailAddress(lineComponents[1], lineComponents[2]);
                        break;
                    default:
                        throw new ArgumentException("The names files contains an invalid line start \"" + lineComponents[0] + "\"", "path2NamesFile");
                }
            }
        }

        private Dictionary<string, ISet<string>> AuthorMapping = new Dictionary<string, ISet<string>>(StringComparer.InvariantCultureIgnoreCase);

        public void InitializeMappingFromAuthorList(IEnumerable<string> linesWithAuthors)
        {
            foreach (string authorLine in linesWithAuthors)
            {
                ISet<string> authorAliasSet = new HashSet<string>();
                foreach (Author similarAuthor in Author.GetAuthorsFromLine(authorLine))
                {
                    authorAliasSet.Add(similarAuthor.completeName);
                    if (null != similarAuthor.NamePart)
                        authorAliasSet.Add(similarAuthor.NamePart);
                    if (null != similarAuthor.MailPart)
                        authorAliasSet.Add(similarAuthor.MailPart);
                    if (null != similarAuthor.LoginNamePart)
                        authorAliasSet.Add(similarAuthor.LoginNamePart);
                }
                ModuloNewSetIntoMultiDictionary(AuthorMapping, authorAliasSet);
            }
        }

        private void LinkMailAddressAndNameWithConsistency(string mailAddress, string name)
        {
            if (Add2CollectionDictionary(mailaddress2name, mailAddress, name))  // alternative email addresses are now linked to the name already, as they reference the same list
            {   // There is already a mail address in the collection, so there is at least one alternative name.
                string oneAlternativeName = mailaddress2name[mailAddress].First(foundName => foundName != name);
                if (!Add2CollectionDictionary(name2name, oneAlternativeName, name))
                    name2name[oneAlternativeName].Add(oneAlternativeName);
                name2name[name] = name2name[oneAlternativeName];
                // now all alternative names in name2name point to the same list, which contains all names
            }

            if (Add2CollectionDictionary(name2mailaddress, name, mailAddress)) // alternative names are now linked to the mail address, as they have the same list reference
            {   // The other mail addresses may not know of each other, though
                string oneAlternativeMail = name2mailaddress[name].First(foundMail => foundMail != mailAddress);
                LinkMailAddressWithMailAddress(mailAddress, oneAlternativeMail);
            }
        }

        private void LinkMailAddressWithMailAddress(string mailAddress, string alternativeMail)
        {
            if (!Add2CollectionDictionary(mail2mail, alternativeMail, mailAddress))
                mail2mail[alternativeMail].Add(alternativeMail);
            mail2mail[mailAddress] = mail2mail[alternativeMail];
        }

        /// <summary>
        /// Takes a list of author names and tries to merge names that belong to the same author into one list. 
        /// </summary>
        /// <param name="authorNames">A list of author names, each of which may be a semicolon separated list of names itself.</param>
        /// <returns>A list of all authors, each of which is represented by a list of its names</returns>
        public IEnumerable<IEnumerable<string>> Consolidate(string[] authorNames)
        {
                // maps each name to its list of all alternatives, including itself. The lists contain only valid, used author names, however
            IDictionary<string, ISet<string>> listOfAuthorAliases = new Dictionary<string, ISet<string>>(StringComparer.InvariantCultureIgnoreCase);
                // maps each alias to all corresponding alias, whether or not they are author names
            IDictionary<string, ISet<string>> listOfAllAliases = AuthorMapping;

            IEnumerable<string> flatListOfAuthorNames = authorNames
                .Select(authorLine => authorLine.Split(','))
                .Aggregate<string[],IEnumerable<string>>(new string[] {}, (aggregated, next) => aggregated.Union(next));

            foreach (string authorNameLine in authorNames)
            {
                if (string.IsNullOrWhiteSpace(authorNameLine))
                    continue;

                ISet<string> foundAliases = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

                foreach (string authorName in authorNameLine.Split(','))
                {
                    foundAliases.Add(authorName);
                    
                    Author currentAuthor = new Author(authorName);
                    if (null != currentAuthor.MailPart)
                    {
                        foundAliases.Add(currentAuthor.MailPart);
                        string usernamepart = currentAuthor.MailPart.Split('@')[0];
                        if (!IsNameVeryCommon(usernamepart))
                            foundAliases.Add(usernamepart);
                        if (usernamepart.Contains('+'))
                        {
                            string realUserNamePart = usernamepart.Split('+')[0];
                            if (!IsNameVeryCommon(realUserNamePart))
                                foundAliases.Add(realUserNamePart);
                        }

                        if (mailaddress2name.ContainsKey(currentAuthor.MailPart))
                            foreach (string alternativeName in mailaddress2name[currentAuthor.MailPart])
                                foundAliases.Add(alternativeName);

                        if (mail2mail.ContainsKey(currentAuthor.MailPart))
                            foreach (string alternativeAddress in mail2mail[currentAuthor.MailPart])
                                if (alternativeAddress != currentAuthor.MailPart)
                                    foundAliases.Add(alternativeAddress);
                    }

                    if (null != currentAuthor.NamePart)
                    {
                        if (!IsNameVeryCommon(currentAuthor.NamePart))
                            foundAliases.Add(currentAuthor.NamePart);

                        if (name2name.ContainsKey(currentAuthor.NamePart))
                            foreach (string alternativeName in name2name[currentAuthor.NamePart])
                                if (alternativeName != currentAuthor.NamePart)
                                    foundAliases.Add(alternativeName);

                        if (name2mailaddress.ContainsKey(currentAuthor.NamePart))
                            foreach (string alternativeMail in name2mailaddress[currentAuthor.NamePart])
                                foundAliases.Add(alternativeMail);
                    }

                    if (null != currentAuthor.LoginNamePart)
                        foundAliases.Add(currentAuthor.LoginNamePart);

                        // acquire all aliases for all aliases
                    ModuloNewSetIntoMultiDictionary(listOfAllAliases, foundAliases);

                    ISet<string> aliases4CurrentName;
                    if (foundAliases.Any(alias => listOfAuthorAliases.ContainsKey(alias)))
                        aliases4CurrentName = listOfAuthorAliases[foundAliases.First(alias => listOfAuthorAliases.ContainsKey(alias))];
                    else
                        aliases4CurrentName = new HashSet<string>();

                    aliases4CurrentName.Add(authorName);
                    foreach (string aliasName in foundAliases)      // merge name collections
                    {
                        if (listOfAuthorAliases.ContainsKey(aliasName)
                                && listOfAuthorAliases[aliasName] != aliases4CurrentName)
                            aliases4CurrentName.UnionWith(listOfAuthorAliases[aliasName]);
                        listOfAuthorAliases[aliasName] = aliases4CurrentName;
                    }
                }
            }

            return listOfAuthorAliases.Values.Distinct();
        }

        private static readonly string[] commonNames = new string[] { "chris", "philipp", "raymond", "robert", "stephen", "thomas", "anton", "bernd",
            //"adam", "ben", "ian", "jan", "jeff", "matt", "paul", "neil",
            //"me", 
            "bugzilla", "mozilla", "bugmail" };

        /// <summary>
        /// Checks whether a name is sufficiently unique to identify someone. If it is too common,
        /// it may belong to multiple people.
        /// </summary>
        private static bool IsNameVeryCommon(string usernamepart)
        {
            return usernamepart.Length < 5 || commonNames.Contains(usernamepart, StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Inserts a set into a dictionary, such that all set entries are keys that point to the set. If, however, 
        /// the dictionary contains keys already that match any of the inserted keys, then any entries in the sets these keys 
        /// point to, will also be added to the set, and those entries will also point to the new set.
        /// If the following conditions hold before executing the method, they will also hold afterwards:
        ///     - every key in the dictionary will point to a set that contains itself and possible other keys,
        ///     - every key will exist in only one of the value sets of the dictionary.
        /// </summary>
        /// <param name="listOfAllAliases">A dictionary of sets (every key maps to a set)</param>
        /// <param name="foundAliases">A set that will be inserted</param>
        private static void ModuloNewSetIntoMultiDictionary(IDictionary<string, ISet<string>> listOfAllAliases, ISet<string> foundAliases)
        {
            var listOfOtherAliasSets = foundAliases
                                        .Where(alias => listOfAllAliases.ContainsKey(alias))
                                        .Select(alias => listOfAllAliases[alias])
                                        .Distinct()
                                        .ToList();      // modifying foundAliases is not allowed without ToList()
            foreach (ISet<string> otherAliasSet in listOfOtherAliasSets)
                foundAliases.UnionWith(otherAliasSet);
            foreach (string alias in foundAliases)
                listOfAllAliases[alias] = foundAliases;
        }
    }
}
