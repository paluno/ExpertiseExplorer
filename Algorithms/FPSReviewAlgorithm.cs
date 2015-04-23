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

        private FPS.RootDirectory FpsTree { get; set; }


        public FPSReviewAlgorithm(FPS.RootDirectory fpsTree)
        {
            Guid = new Guid("F1C17EA9-81E8-4F2B-A08D-A2DBC056F36D");
            this.FpsTree = fpsTree;
            Init(30);
        }

        public override void CalculateExpertiseForFile(string filename)
        {

            IDictionary<String, Double> dictExpertiseValues = FpsTree.CalculateDeveloperExpertisesForFile(filename);

            using (var repository = new ExpertiseDBEntities())
            {
                int artifactId = FindOrCreateArtifact(repository,filename, ArtifactTypeEnum.File).ArtifactId;

                foreach (KeyValuePair<String, Double> pair in dictExpertiseValues)
                {
                    repository.StoreDeveloperExpertiseValue(pair.Key, pair.Value, artifactId, RepositoryId, AlgorithmId);
                    //Developer developer = repository.Developers.SingleOrDefault(dev => dev.Name == pair.Key && dev.RepositoryId == RepositoryId);
                    //DeveloperExpertise developerExpertise = FindOrCreateDeveloperExpertise(repository, developer.DeveloperId, filename, ArtifactTypeEnum.File);
                    //DeveloperExpertiseValue devExpertiseValue = FindOrCreateDeveloperExpertiseValue(repository, developerExpertise);
                    //devExpertiseValue.Value = pair.Value;
                    //repository.SaveChanges();
                }
            }
        }
    }
}
