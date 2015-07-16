namespace ExpertiseExplorer.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.IO;
    using System.Linq;

    using ExpertiseDB;

    /// <summary>
    /// Based on Fritz et al.'s Degree-of-Knowledge model published 2010 at ICSE and 2014 in TOSEM
    /// </summary>
    public class DegreeOfAuthorshipAlgorithm : AlgorithmBase
    {
        /// <summary>
        /// Which weightings should be used for the formula?
        /// </summary>
        public enum WeightingType { 
            /// <summary>
            /// This selects the weightings from the original ICSE'10 paper "A Degree-of-Knowledge Model to Capture Source Code Familiarity"
            /// </summary>
            Original, 
            /// <summary>
            /// This value represents the weightings from the 2014 TOSEM article "Degree-of-knowledge: Modeling a Developer's Knowledge of Code", Sec. 8.2
            /// </summary>
            UniversalTOSEM 
        }

        private readonly double firstAuthorWeighting;
        private readonly double delivieresWeighting;
        private readonly double acceptanceWeighting;
        private readonly double constantSummand;

        public DegreeOfAuthorshipAlgorithm(WeightingType sourceOfWeightings)
        {
            Name += "-" + sourceOfWeightings.ToString();
            switch(sourceOfWeightings)
            {
                case WeightingType.Original:
                    Guid = new Guid("59a9d58a-8382-43b1-a438-ddb9a154dda9");
                    firstAuthorWeighting = 1.098d;
                    delivieresWeighting = 0.164d;
                    acceptanceWeighting = -0.321d;
                    constantSummand = 3.293d;
                    break;
                case WeightingType.UniversalTOSEM:
                    Guid = new Guid("5C11F944-B959-45D2-80DB-EAB141437631");
                    firstAuthorWeighting = 0.962d;
                    delivieresWeighting = 0.213d;
                    acceptanceWeighting = -0.273d;
                    constantSummand = 3.223d;
                    break;
                default:
                    throw new ArgumentException("Unknown type of weighting " + sourceOfWeightings);
            }
            Init();
        }
        public override void UpdateFromSourceUntil(DateTime end)
        {
            SourceRepositoryManager.BuildConnectionsForSourceRepositoryUntil(end);
            base.UpdateFromSourceUntil(end);
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            List<int> allExpertiseIDs;
            int artifactId = SourceRepositoryManager.FindOrCreateFileArtifactId(filename);

            using (var repository = new ExpertiseDBEntities())
            {
                allExpertiseIDs = repository.DeveloperExpertises.Include(de => de.Artifact)
                    .Where(de => de.Artifact.RepositoryId == RepositoryId && de.Artifact.ArtifactId == artifactId
                        && de.Inferred == false && (de.DeliveriesCount>0 || de.IsFirstAuthor))  // this filters reset DeveloperExpertises with no direct expertise
                    .Select(de => de.DeveloperExpertiseId).ToList();
            }

            foreach (var developerExpertiseId in allExpertiseIDs)
            {
                using (var repository = new ExpertiseDBEntities())
                {
                    DeveloperExpertise developerExpertise = repository.DeveloperExpertises.Include(de => de.Artifact).Include(de => de.DeveloperExpertiseValues).Single(de => de.DeveloperExpertiseId == developerExpertiseId);

                    int firstAuthorship = developerExpertise.IsFirstAuthor ? 1 : 0;

                    double fistAuthorshipValue = firstAuthorWeighting * firstAuthorship;

                    double deliveriesValue = delivieresWeighting * developerExpertise.DeliveriesCount;

                    double acceptancesValue = acceptanceWeighting * Math.Log(1 + developerExpertise.Artifact.ModificationCount - (developerExpertise.DeliveriesCount + firstAuthorship));

                    double expertise = constantSummand + fistAuthorshipValue + deliveriesValue + acceptancesValue;

                    DeveloperExpertiseValue expertiseValue = FindOrCreateDeveloperExpertiseValue(developerExpertise);

                    expertiseValue.Value = expertise;

                    repository.SaveChanges();
                }
            }
        }
    }
}
