using ExpertiseDB;
using ExpertiseExplorerCommon;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExpertiseDB.Extensions;

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
            Init();
        }

        public override void CalculateExpertiseForFile(string filename)
        {

            IDictionary<String, Double> dictExpertiseValues = FpsTree.CalculateDeveloperExpertisesForFile(filename);

            IEnumerable<DeveloperWithExpertise> devIdsWithExpertiseValues;
            using (var repository = new ExpertiseDBEntities())
            {
                devIdsWithExpertiseValues = dictExpertiseValues.
                    Select(devNameExpertisePair => new DeveloperWithExpertise(
                            repository.Developers.Single(dev => dev.Name == devNameExpertisePair.Key && dev.RepositoryId == RepositoryId)
                                .DeveloperId,
                            devNameExpertisePair.Value
                        )).ToList();     // convert developer names into DeveloperIds
            }
            storeDeveloperExpertiseValues(filename, devIdsWithExpertiseValues);
        }
    }
}
