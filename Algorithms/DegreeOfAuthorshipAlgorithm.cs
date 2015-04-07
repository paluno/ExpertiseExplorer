namespace Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.IO;
    using System.Linq;

    using ExpertiseDB;

    public class DegreeOfAuthorshipAlgorithm : AlgorithmBase
    {
        public DegreeOfAuthorshipAlgorithm()
        {
            Guid = new Guid("59a9d58a-8382-43b1-a438-ddb9a154dda9");
            Init();
        }

        public override void CalculateExpertise()
        {
            List<int> allExpertiseIDs;
            using (var repository = new ExpertiseDBEntities())
            {
                allExpertiseIDs = repository.DeveloperExpertises.Include(de => de.Developer).Where(de => de.Developer.RepositoryId == RepositoryId && de.Inferred == false).Select(de => de.DeveloperExpertiseId).ToList();
            }

            foreach (var developerExpertiseId in allExpertiseIDs)
            {
                using (var repository = new ExpertiseDBEntities())
                {
                    var developerExpertise = repository.DeveloperExpertises.Include(de => de.Artifact).Include(de => de.DeveloperExpertiseValues).Single(de => de.DeveloperExpertiseId == developerExpertiseId);

                    var firstAuthorship = developerExpertise.IsFirstAuthor ? 1 : 0;

                    var fistAuthorshipValue = 1.098d * firstAuthorship;

                    var deliveriesValue = 0.164d * developerExpertise.DeliveriesCount;

                    var acceptancesValue = 0.321 * Math.Log(1 + developerExpertise.Artifact.ModificationCount - (developerExpertise.DeliveriesCount + firstAuthorship));

                    var expertise = 3.293d + fistAuthorshipValue + deliveriesValue - acceptancesValue;

                    var expertiseValue =
                        developerExpertise.DeveloperExpertiseValues.SingleOrDefault(
                            dev => dev.AlgorithmId == AlgorithmId) ?? repository.DeveloperExpertiseValues.Add(
                                new DeveloperExpertiseValue
                                    {
                                        AlgorithmId = AlgorithmId,
                                        DeveloperExpertiseId = developerExpertise.DeveloperExpertiseId
                                    });

                    expertiseValue.Value = expertise;

                    repository.SaveChanges();
                }
            }
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            List<int> allExpertiseIDs;
            var artifactId = GetArtifactIdFromArtifactnameApproximation(filename);
            if (artifactId < 0)
                throw new FileNotFoundException(string.Format("Filename {0} not found", filename));

            using (var repository = new ExpertiseDBEntities())
            {
                allExpertiseIDs = repository.DeveloperExpertises.Include(de => de.Artifact).Where(de => de.Artifact.RepositoryId == RepositoryId && de.Artifact.ArtifactId == artifactId && de.Inferred == false).Select(de => de.DeveloperExpertiseId).ToList();
            }

            foreach (var developerExpertiseId in allExpertiseIDs)
            {
                using (var repository = new ExpertiseDBEntities())
                {
                    var developerExpertise = repository.DeveloperExpertises.Include(de => de.Artifact).Include(de => de.DeveloperExpertiseValues).Single(de => de.DeveloperExpertiseId == developerExpertiseId);

                    var firstAuthorship = developerExpertise.IsFirstAuthor ? 1 : 0;

                    var fistAuthorshipValue = 1.098d * firstAuthorship;

                    var deliveriesValue = 0.164d * developerExpertise.DeliveriesCount;

                    var acceptancesValue = 0.321 * Math.Log(1 + developerExpertise.Artifact.ModificationCount - (developerExpertise.DeliveriesCount + firstAuthorship));

                    var expertise = 3.293d + fistAuthorshipValue + deliveriesValue - acceptancesValue;

                    var expertiseValue = FindOrCreateDeveloperExpertiseValue(repository, developerExpertise);

                    expertiseValue.Value = expertise;

                    repository.SaveChanges();
                }
            }
        }
    }
}
