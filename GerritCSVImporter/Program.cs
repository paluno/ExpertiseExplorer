using ExpertiseDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GerritCSVImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                ShowHelp();
                return;
            }

            processCSV(args[0], args[1]);
        }

        public static void ShowHelp()
        {
            Console.WriteLine("Usage: GerritCSVImporter.exe RepositoryName pathToCSVFile");
        }

        private static void processCSV(string repositoryName, string filename)
        {
            int repositoryId = getRepositoryId(repositoryName);

            string[] csvLines = File.ReadAllLines(filename);

            foreach (string csvLine in csvLines)
            {
                string[] csvValues = csvLine.Split(';');


                int revisionId;
                using (var repository = new ExpertiseDBEntities())
                {
                    var idString = csvValues[0];
                    var revision = repository.Revisions.SingleOrDefault(r => r.SourceRepositoryId == repositoryId && r.ID == idString);
                    if (revision == null)
                    {
                        revision = repository.Revisions.Add(
                                new Revision
                                {
                                    ID = idString,
                                    SourceRepositoryId = repositoryId,
                                    Time = DateTime.Parse(csvValues[1]),
                                    User = csvValues[2]
                                });

                        repository.SaveChanges();
                        revisionId = revision.RevisionId;
                    }
                    else
                    {
                        Console.WriteLine("Skipping Revison: " + revision.ID);
                        continue;
                    }
                }

                string[] modifiedFiles = csvValues[3].Split(',');

                foreach (string fileModification in modifiedFiles)
                {
                    string[] fileModificationData = fileModification.Split(':');

                    string fileName = fileModificationData[0];
                    int addedLines = int.Parse(fileModificationData[1]);
                    int deletedLines = int.Parse(fileModificationData[2]);
                    bool isNew = bool.Parse(fileModificationData[3]);

                    FileRevision file = new FileRevision
                    {
                        IsNew = isNew,
                        LinesAdded = addedLines,
                        LinesDeleted = deletedLines,
                        RevisionId = revisionId,
                        SourceRepositoryId = repositoryId
                    };

                    AddFileRevision(file, fileName);
                }
            }


        }

        private static int getRepositoryId(string repositoryName)
        {
            using (ExpertiseDBEntities repository = new ExpertiseDBEntities())
            {
                SourceRepository sourceRepository = repository.SourceRepositorys.SingleOrDefault(sr => sr.URL == repositoryName);
                if (sourceRepository == null)
                {
                    sourceRepository = repository.SourceRepositorys.Add(new SourceRepository { Name = repositoryName, URL = repositoryName });
                    repository.SaveChanges();
                }
                return sourceRepository.SourceRepositoryId;
            }


        }

        private static void AddFileRevision(FileRevision fileRevision, string name)
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
