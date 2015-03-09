namespace Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using ExpertiseDB;

    class FilePathSimilarity : AlgorithmBase
    {
        const double TIME_PRIORITIZATION = 1d;

        public FilePathSimilarity()
        {
            Guid = new Guid("FilePathSimilarity");
            Init();
        }


        public override void CalculateExpertise()
        {
            Debug.Assert(MaxDateTime != DateTime.MinValue, "Initialize MaxDateTime first");
            Debug.Assert(SourceRepositoryId > -1, "Initialize SourceRepositoryId first");
            Debug.Assert(RepositoryId > -1, "Initialize RepositoryId first");

            List<string> filenames;
            using (var repository = new ExpertiseDBEntities())
            {
                filenames = repository.Artifacts.Select(a => a.Name).ToList();
            }
            CalculateExpertiseForFiles(filenames);
        }

        public override void CalculateExpertiseForFile(string filename)
        {
            var stopwatch = new Stopwatch();

            var filenameId = GetFilenameIdFromFilenameApproximation(filename);
            if (filenameId < 0)
            {
                throw new FileNotFoundException(string.Format("Filename {0} not found", filename));
            }

            var artifactId = GetArtifactIdFromArtifactnameApproximation(filename);
            if (artifactId < 0)
            {
                throw new FileNotFoundException(string.Format("Artifact {0} not found", filename));
            }

            // Load latest file revision 
            FileRevision fileRevision;
            stopwatch.Start();

            using (var repository = new ExpertiseDBEntities())
            {
                fileRevision = repository.FileRevisions.Include(fr => fr.Revision).Where(f => f.FilenameId == filenameId && f.Revision.Time <= MaxDateTime).OrderByDescending(f => f.Revision.Time).AsNoTracking().First();
            }

            DateTime revisionDateTime = fileRevision.Revision.Time;

            // Load all old revisions
            List<Revision> oldRevisions;

            using (var repository = new ExpertiseDBEntities())
            {
                oldRevisions = repository.Revisions.Include(f => f.Files).Where(f => f.Time < revisionDateTime).OrderByDescending(f => f.Time).AsNoTracking().ToList();
            }



            // FIX!
            IOrderedEnumerable<KeyValuePair<string, double>> recommendReviewers = RecommendReviewers(fileRevision, oldRevisions, TIME_PRIORITIZATION);

            using (var entities = new ExpertiseDBEntities())
            {
                foreach (KeyValuePair<string, double> pair in recommendReviewers)
                {

                    var developer = entities.Developers.Single(dev => dev.Name == pair.Key);
                    int developerExpertiseId = developer.DeveloperId;


                    var expertiseValue = entities.DeveloperExpertiseValues.SingleOrDefault(
                                    dev => dev.AlgorithmId == AlgorithmId && dev.DeveloperExpertiseId == developerExpertiseId) ?? entities.DeveloperExpertiseValues.Add(
                                        new DeveloperExpertiseValue
                                        {
                                            AlgorithmId = AlgorithmId,
                                            DeveloperExpertiseId = developerExpertiseId
                                        });
                    expertiseValue.Value = pair.Value;
                }
                entities.SaveChanges();
            }

        }



        private IOrderedEnumerable<KeyValuePair<string, double>> RecommendReviewers(FileRevision filesNewReviewRequest, List<Revision> pastReviews, double timePrioritization)
        {
            List<string> filesNewReviewRequestFilenames = new List<string>();
            filesNewReviewRequestFilenames.Add(filesNewReviewRequest.Filename.Name);

            var scores = new Dictionary<string, double>();
            int m = 0;
            foreach (Revision pastReview in pastReviews)
            {

                List<string> pastReviewFilenames = new List<string>();
                foreach (FileRevision fileRevision in pastReview.Files)
                    pastReviewFilenames.Add(fileRevision.Filename.Name);

                double score = FPS(filesNewReviewRequestFilenames, pastReviewFilenames, timePrioritization, m);
                string user = pastReview.User; // only 1 user?

                if (!scores.ContainsKey(user))
                    scores.Add(user, 0);

                scores[user] += score;
            }

            var descendingScores = from pair in scores
                                   orderby pair.Value descending
                                   select pair;


            return descendingScores;
        }


        private double FPS(List<string> filesNewReviewRequest, List<string> filesPastReviewRequest, double timePrioritization, int m)
        {
            double dividend = 0;

            foreach (string fileNewReviewRequest in filesNewReviewRequest)
            {
                foreach (string filePastReviewRequest in filesPastReviewRequest)
                {
                    double similarity = Similarity(fileNewReviewRequest, filePastReviewRequest);
                    dividend = dividend + similarity;
                }
            }

            double divisor = filesNewReviewRequest.Count * filesPastReviewRequest.Count;
            double result = dividend / divisor * Math.Pow(timePrioritization, m);
            return result;
        }

        private double Similarity(String path1, String path2)
        {
            string[] directories1 = path1.Split(Path.DirectorySeparatorChar);
            string[] directories2 = path2.Split(Path.DirectorySeparatorChar);

            double commonPathResult = CommonPath(path1, path2);
            double maxLength = Math.Max(directories1.Length, directories2.Length);
            double result = commonPathResult / maxLength;
            return result;
        }

        private int CommonPath(String path1, String path2)
        {
            int result = 0;
            string[] directories1 = path1.Split(Path.DirectorySeparatorChar);
            string[] directories2 = path2.Split(Path.DirectorySeparatorChar);

            for (int i = 0; i < Math.Min(directories1.Length, directories2.Length); i++)
            {

                if (!directories1[i].Equals(directories2[i], StringComparison.OrdinalIgnoreCase))
                    return result;

                result++;
            }

            return result;
        }

        class Review
        {
            public string[] files { get; set; }
            public long[] reviewerIDs { get; set; }

            Review(string[] files, long[] reviewerIDs)
            {
                this.files = files;

                this.reviewerIDs = reviewerIDs;
            }
        }

    }
}
