namespace Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.IO;
    using System.Linq;

    using ExpertiseDB;

    public class ExperienceAtomsAlgorithm : AlgorithmBase
    {
        public ExperienceAtomsAlgorithm()
        {
            Guid = new Guid("ee251be9-605c-490a-b04c-540fe05e2b68");
            Init();
        }

        public override void CalculateExpertise()
        {
            List<string> filenames;
            using (var repository = new ExpertiseDBEntities())
            {
                filenames = repository.Artifacts.Select(a => a.Name).ToList();
            }

            CalculateExpertiseForFiles(filenames);
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            var artifactId = GetArtifactIdFromArtifactnameApproximation(filename);
            if (artifactId < 0)
            {
                throw new FileNotFoundException(string.Format("Artifact {0} not found", filename));
            }

            using (var repository = new ExpertiseDBEntities())
            {
                var developers = repository.DeveloperExpertises.Where(de => de.ArtifactId == artifactId && de.Inferred == false).Select(de => de.DeveloperId).Distinct().ToList();

                foreach (var developerId in developers)
                {
                    var developerExpertise = repository.DeveloperExpertises.Include(de => de.DeveloperExpertiseValues).Single(de => de.DeveloperId == developerId && de.ArtifactId == artifactId);

                    var expertiseValue =
                        developerExpertise.DeveloperExpertiseValues.SingleOrDefault(
                            dev => dev.AlgorithmId == AlgorithmId) ?? repository.DeveloperExpertiseValues.Add(
                                new DeveloperExpertiseValue
                                {
                                    AlgorithmId = AlgorithmId,
                                    DeveloperExpertiseId = developerExpertise.DeveloperExpertiseId
                                });

                    expertiseValue.Value = developerExpertise.DeliveriesCount + (developerExpertise.IsFirstAuthor ? 1f : 0f);
                }

                repository.SaveChanges();
            }
        }
    }
}
