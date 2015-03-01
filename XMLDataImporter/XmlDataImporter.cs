namespace XMLDataImporter
{
    using System;
    using System.Data.Entity.Validation;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using ExpertiseDB;
    using ExpertiseExplorerCommon;

    public class XmlDataImporter
    {
        public void ProcessDirectory(string path)
        {
            try
            {
                Debug.WriteLine("Starting Import at: " + DateTime.Now);
                var files = Directory.GetFiles(path, "*.xml").OrderBy(f => new FileInfo(f).Name).ToList();

                int sourceRepositoryId;
                using (var input = new StreamReader(files[0]))
                {
                    using (var repository = new ExpertiseDBEntities())
                    {
                        var formatter = new XmlSerializer(typeof(RepositoryImage));
                        var result = (RepositoryImage)formatter.Deserialize(input);
                        var sourceRepository = repository.SourceRepositorys.SingleOrDefault(sr => sr.URL == result.Source);
                        if (sourceRepository == null)
                        {
                            sourceRepository = repository.SourceRepositorys.Add(new SourceRepository { Name = result.Source, URL = result.Source });
                            repository.SaveChanges();
                        }

                        sourceRepositoryId = sourceRepository.SourceRepositoryId;
                    }
                }

                foreach (var file in files)
                {
                    ProcessXmlFile(file, sourceRepositoryId);
                }
            }
            catch (DbEntityValidationException e)
            {
                foreach (var eve in e.EntityValidationErrors)
                {
                    Console.WriteLine(
                        "Entity of type \"{0}\" in state \"{1}\" has the following validation errors:",
                        eve.Entry.Entity.GetType().Name,
                        eve.Entry.State);
                    foreach (var ve in eve.ValidationErrors)
                    {
                        Console.WriteLine(
                            "- Property: \"{0}\", Error: \"{1}\"",
                            ve.PropertyName,
                            ve.ErrorMessage);
                    }
                }

                throw;
            }

            Debug.WriteLine("Finishing Import at: " + DateTime.Now);
        }

        private void ProcessXmlFile(string pathToFile, int sourceRepositoryId)
        {
            using (var input = new StreamReader(pathToFile))
            {
                var formatter = new XmlSerializer(typeof(RepositoryImage));
                var result = (RepositoryImage)formatter.Deserialize(input);
                foreach (var inputRevision in result.Changesets)
                {
                    int revisionId;
                    using (var repository = new ExpertiseDBEntities())
                    {
                        var idString = inputRevision.Id.ToString();
                        var revision = repository.Revisions.SingleOrDefault(r => r.SourceRepositoryId == sourceRepositoryId && r.ID == idString);
                        if (revision == null)
                        {
                            revision = repository.Revisions.Add(
                                    new Revision
                                    {
                                        Description = inputRevision.Description,
                                        ID = inputRevision.Id.ToString(),
                                        Parent = inputRevision.Parents,
                                        SourceRepositoryId = sourceRepositoryId,
                                        Tag = inputRevision.Tag,
                                        Time = inputRevision.Date,
                                        User = inputRevision.User
                                    });

                            repository.SaveChanges();
                            revisionId = revision.RevisionId;
                        }
                        else
                        {
                            Debug.WriteLine("Skipping Revison: " + revision.ID);
                            continue;
                        }
                    }

                    foreach (var fileChange in inputRevision.Files)
                    {
                        var file = new FileRevision
                        {
                            Diff =
                                fileChange.IsBinary
                                    ? null
                                    : System.Text.Encoding.ASCII.GetString(
                                        Convert.FromBase64String(fileChange.RawDiff)),
                            IsNew = fileChange.IsNew,
                            LinesAdded = fileChange.LinesAdded,
                            LinesDeleted = fileChange.LinesDeleted,
                            RevisionId = revisionId,
                            SourceRepositoryId = sourceRepositoryId
                        };

                        AddFileRevision(file, fileChange.Name);
                    }
                }
            }
        }

        private void AddFileRevision(FileRevision fileRevision, string name)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                var filename = repository.Filenames.SingleOrDefault(a => a.Name == name && a.SourceRepositoryId == fileRevision.SourceRepositoryId)
                                  ??
                                  new Filename { Name = name, SourceRepositoryId = fileRevision.SourceRepositoryId };

                fileRevision.Filename = filename;
                repository.FileRevisions.Add(fileRevision);
                repository.SaveChanges();
            }
        }
    }
}
