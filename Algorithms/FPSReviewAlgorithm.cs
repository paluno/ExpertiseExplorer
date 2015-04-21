using ExpertiseDB;
using ExpertiseExplorerCommon;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Algorithms
{
    /// <summary>
    /// Calculates Thongtanunam et al.s FPS algorithm for reviewer recommendation in the delta=1.0 parameterization
    /// </summary>
    public class FPSReviewAlgorithm : AlgorithmBase
    {
        public FPSReviewAlgorithm()
        {
            Guid = new Guid("F1C17EA9-81E8-4F2B-A08D-A2DBC056F36D");
            Init(30);
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            string[] fileNameComponents = filename.Split('/');
            string firstComponent = fileNameComponents[0] + "/";

            int weighedReviewAlgorithmId;
            Dictionary<int,double> similarFiles;
            IEnumerable<int> experiencedDevelopersIds;

            using (var repository = new ExpertiseDBEntities())
            {
                // Find the WeighedReviewCountAlgorithm, it's the basis for further calculations.
                weighedReviewAlgorithmId = repository.Algorithms
                    .Single(algo => algo.GUID == WeighedReviewCountAlgorithm.WEIGHEDREVIEWCOUNTGUID)
                    .AlgorithmId;

                    // Find files that match at least with the first component
                    // Index is the ArtifactID, value is the similarity to the given file
                similarFiles = repository.Artifacts
                    .Where(artifact => artifact.Name.StartsWith(firstComponent) && artifact.ArtifactTypeId == (int)ArtifactTypeEnum.File)
                    .ToDictionary(
                        artifact => artifact.ArtifactId, 
                        artifact => FileSimilarity(fileNameComponents, artifact.Name.Split('/'))
                     );

                    // Find all developers who have some experience with the files

                //experiencedDevelopers = repository.Developers
                //    .Include("DeveloperExpertises.DeveloperExpertiseValues")
                //    .Where(dev => dev.RepositoryId == RepositoryId &&
                //                  dev.DeveloperExpertises.Any(devExpertise => 
                //                      devExpertise.Artifact.Name.StartsWith(firstComponent) &&
                //                      devExpertise.Artifact.ArtifactTypeId == (int) ArtifactTypeEnum.File &&
                //                      devExpertise.DeveloperExpertiseValues.Any(devExValue => devExValue.AlgorithmId == weighedReviewAlgorithmId)
                //                  )
                //    ).ToList();
                using (IDbConnection con = repository.Database.Connection)
                {
                    con.Open();
                    using (IDbCommand com = con.CreateCommand())
                    {
                        com.CommandText = "SELECT DISTINCT DeveloperExpertises.DeveloperId " +
                            "FROM DeveloperExpertises " +
                            "INNER JOIN Artifacts ON DeveloperExpertises.ArtifactId = Artifacts.ArtifactId " +
                            "INNER JOIN DeveloperExpertiseValues ON DeveloperExpertises.DeveloperExpertiseId = DeveloperExpertiseValues.DeveloperExpertiseId " +
                            "WHERE RepositoryId=@RepositoryId " +
                            "AND ArtifactTypeId=@ArtifactTypeId " +
                            "AND DeveloperExpertiseValues.AlgorithmId=@AlgorithmId " +
                            "AND Name LIKE @FileNameStart";

                        com.addDBParameter("@RepositoryId", DbType.Int32, RepositoryId);
                        com.addDBParameter("@ArtifactTypeId", DbType.Int32, ArtifactTypeEnum.File);
                        com.addDBParameter("@AlgorithmId", DbType.Int32, weighedReviewAlgorithmId);
                        com.addDBParameter("@FileNameStart", DbType.String, firstComponent + "%");

                        DataTable dtDevelopers = new DataTable();
                        dtDevelopers.Load(com.ExecuteReader());
                        experiencedDevelopersIds = dtDevelopers.Rows.OfType<DataRow>().Select<DataRow, int>(row => (int)row["DeveloperId"]);
                    }
                }
            }

            List<Task> tasks = new List<Task>();
            foreach (int developerId in experiencedDevelopersIds)
            {
                tasks.Add(
                    TaskFactory.StartNew(
                        input => CalculateExpertiseForDeveloper((int)input, weighedReviewAlgorithmId, firstComponent, filename, similarFiles),
                        developerId,
                            TaskCreationOptions.AttachedToParent));
            }

            Task.WaitAll(tasks.ToArray());
        }

        private void CalculateExpertiseForDeveloper(int developerId, int weighedReviewAlgorithmId, string firstComponent, string filename, Dictionary<int,double> similarFiles)
        {
            using (var repository = new ExpertiseDBEntities())
            {
                double developerFPSExpertiseValue =
                    repository.DeveloperExpertiseValues
                        .Include("DeveloperExpertise")
                    // All WeighedReviewCount expertises of the developer for relevant files
                        .Where(devExpValue => devExpValue.DeveloperExpertise.DeveloperId == developerId
                                && devExpValue.AlgorithmId == weighedReviewAlgorithmId
                                && //similarFiles.ContainsKey(devExpValue.DeveloperExpertise.ArtifactId)    // This might be very slow
                                    devExpValue.DeveloperExpertise.Artifact.Name.StartsWith(firstComponent) // The same as for similarFiles, but can be translated to SQL
                                && devExpValue.DeveloperExpertise.Artifact.ArtifactTypeId == (int)ArtifactTypeEnum.File
                                )
                        .ToArray()  // execute query, as otherwise LINQ wants to Aggregate, but throws an exception as it cannot
                    // Sum up the file similarities weighed by the individual developer's review expertise with the files
                        .Aggregate<DeveloperExpertiseValue, double>(0D, (accumulated, devExpValue) => accumulated + devExpValue.Value * similarFiles[devExpValue.DeveloperExpertise.ArtifactId]);

                DeveloperExpertise developerExpertise = FindOrCreateDeveloperExpertise(repository, developerId, filename, ArtifactTypeEnum.File);
                DeveloperExpertiseValue devExpertiseValue = FindOrCreateDeveloperExpertiseValue(repository, developerExpertise);
                devExpertiseValue.Value = developerFPSExpertiseValue;
                repository.SaveChanges();
            }

        }

        /// <summary>
        /// Calculates the file similarity index: How many directory components of a file name from left to right match, as divided by the
        /// maximum path length. Assumes that the first component matches already.
        /// </summary>
        public double FileSimilarity(string[] fileName1Components, string[] fileName2Components)
        {
            int indexOfFirstMismatch = 1;
            
            while (indexOfFirstMismatch < fileName1Components.Length &&
                   indexOfFirstMismatch < fileName2Components.Length &&
                   fileName1Components[indexOfFirstMismatch] == fileName2Components[indexOfFirstMismatch])
                ++indexOfFirstMismatch;
            
            return (indexOfFirstMismatch - 1) / (double)Math.Max(fileName1Components.Length, fileName2Components.Length);
        }
    }
}
