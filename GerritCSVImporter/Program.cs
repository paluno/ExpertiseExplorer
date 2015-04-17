using ExpertiseDB;
using System;
using System.Collections.Generic;
using System.Data;
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

            using (var repository = new ExpertiseDBEntities())
            {
                HashSet<string> existingRevisions = new HashSet<string>(repository.Revisions.Where(r => r.SourceRepositoryId == repositoryId).Select(r => r.ID));

                int count = 0;
                foreach (string csvLine in csvLines)
                {
                    if (++count % 100 == 0)
                        Console.WriteLine("Item #" + count);

                    string[] csvValues = csvLine.Split(';');

                    if (string.IsNullOrEmpty(csvValues[3])) // merging two branches creates a change, but with no files in it, and no real review
                        continue;

                    string[] modifiedFiles = csvValues[3].Split(',');


                    int revisionId;
                    var idString = csvValues[0];

                    if (existingRevisions.Contains(idString))
                    {
                        Console.WriteLine("Skipping Revison: " + idString);
                        continue;
                    }

                    Revision revision = repository.Revisions.Add(
                            new Revision
                            {
                                ID = idString,
                                SourceRepositoryId = repositoryId,
                                Time = DateTime.Parse(csvValues[1]),
                                User = csvValues[2]
                            });

                    repository.SaveChanges();
                    revisionId = revision.RevisionId;

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

                        AddFileRevision(repository, file, fileName);
                    }
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

        private static void AddFileRevision(ExpertiseDBEntities repository, FileRevision fileRevision, string name)
        {
            var filename = repository.Filenames.SingleOrDefault(a => a.Name == name && a.SourceRepositoryId == fileRevision.SourceRepositoryId);

            //fileRevision.Filename = filename;
            //repository.FileRevisions.Add(fileRevision);
            //repository.SaveChanges();

            using (IDbConnection con = repository.Database.Connection)
            {
                con.Open();
                using (IDbCommand com = con.CreateCommand())
                {
                    com.CommandText = "";
                    if (null == filename)
                        com.CommandText += "INSERT INTO Filenames(Name,SourceRepositoryId) VALUES(@FileName,@SourceRepositoryId);";
                        
                    com.CommandText += "INSERT INTO FileRevisions(IsNew,LinesAdded,LinesDeleted,RevisionId,SourceRepositoryId,FilenameId) " +
                                             "VALUES(@IsNew,@LinesAdded,@LinesDeleted,@RevisionId,@SourceRepositoryId," +
                                                (null==filename?"LAST_INSERT_ID()":"@FilenameId") + ")";

                    if (null == filename)
                        com.addDBParameter("@FileName", DbType.String, name);
                    else
                        com.addDBParameter("@FilenameId", DbType.Int32, filename.FilenameId);
                    com.addDBParameter("@SourceRepositoryId", DbType.Int32, fileRevision.SourceRepositoryId);
                    com.addDBParameter("@IsNew", DbType.Boolean, fileRevision.IsNew);
                    com.addDBParameter("@LinesAdded", DbType.Int32, fileRevision.LinesAdded);
                    com.addDBParameter("@LinesDeleted", DbType.Int32, fileRevision.LinesDeleted);
                    com.addDBParameter("@RevisionId", DbType.Int32, fileRevision.RevisionId);

                    com.ExecuteNonQuery();
                }
            }
        }
    }
}
