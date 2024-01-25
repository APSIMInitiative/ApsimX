using System;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// Summary statistics of a list
    /// Used with ReportRuminantAttributeSummary
    /// </summary>
    [Serializable]
    public class ListStatistics
    {
        /// <summary>
        /// Average of list
        /// </summary>
        public double Average { get; set; }

        /// <summary>
        /// Standard deviation
        /// </summary>
        public double StandardDeviation { get; set; }

        /// <summary>
        /// Average of list
        /// </summary>
        public double AverageMate { get; set; }

        /// <summary>
        /// Standard deviation
        /// </summary>
        public double StandardDeviationMate { get; set; }

        /// <summary>
        /// number of individuals with attribute
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// number of individuals
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// number of individuals with Mate value
        /// </summary>
        public int TotalMate { get; set; }

    }
}
