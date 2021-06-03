using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM
{
    /// <summary>
    /// Summary statistics of a list
    /// </summary>
    [Serializable]
    public class ListStatistics
    {
        /// <summary>
        /// Average of list
        /// </summary>
        public double Average{ get; set; }

        /// <summary>
        /// Standard deviation
        /// </summary>
        public double StandardDeviation { get; set; }

        /// <summary>
        /// number of individuals with attribute
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// number of individuals
        /// </summary>
        public int Total { get; set; }

    }
}
