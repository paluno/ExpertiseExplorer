namespace Statistics
{
    internal class StatisticsResult
    {
        public StatisticsResult(int actualReviewerId)
        {
            ActualReviewerId = actualReviewerId;
            AuthorWasExpertNo = 0;
        }

        public int ActualReviewerId { get; private set; }

        public bool AuthorWasFound
        {
            get
            {
                return AuthorWasExpertNo > 0;
            }
        }

        public int AuthorWasExpertNo { get; set; }

        public double ExpertiseValue { get; set; }

        public static StatisticsResult FromCSVLine(string csvline)
        {
            var tmp = csvline.Split(';');
            var actualReviewerId = int.Parse(tmp[0]);
            var authorWasExpertNo = int.Parse(tmp[1]);
            var expertiseValue = double.Parse(tmp[2]);

            return new StatisticsResult(actualReviewerId) { AuthorWasExpertNo = authorWasExpertNo, ExpertiseValue = expertiseValue };
        }

        public string ToCSV()
        {
            return ActualReviewerId + ";" + AuthorWasExpertNo + ";" + ExpertiseValue;
        }
    }
}