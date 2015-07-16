namespace ExpertiseExplorer.Algorithms
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

        public override void UpdateFromSourceUntil(DateTime end)
        {
            SourceRepositoryManager.BuildConnectionsForSourceRepositoryUntil(end);
            base.UpdateFromSourceUntil(end);
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            int artifactId = SourceRepositoryManager.FindOrCreateFileArtifactId(filename);

            using (var repository = new ExpertiseDBEntities())
            {
                var developers = repository.DeveloperExpertises
                    .Where(de => de.ArtifactId == artifactId && de.Inferred == false && (de.DeliveriesCount>0 || de.IsFirstAuthor))
                    .Select(de => de.DeveloperId)
                    .Distinct().ToList();

                foreach (var developerId in developers)
                {
                    DeveloperExpertise developerExpertise = repository.DeveloperExpertises.Include(de => de.DeveloperExpertiseValues).Single(de => de.DeveloperId == developerId && de.ArtifactId == artifactId);

                    DeveloperExpertiseValue expertiseValue = FindOrCreateDeveloperExpertiseValue(developerExpertise);

                    expertiseValue.Value = developerExpertise.DeliveriesCount + (developerExpertise.IsFirstAuthor ? 1f : 0f);
                }

                repository.SaveChanges();
            }
        }
    }
}
