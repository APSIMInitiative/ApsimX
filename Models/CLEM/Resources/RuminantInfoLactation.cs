using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Store of female ruminant lactation for the time-step
    /// </summary>
    [Serializable]
    public class RuminantInfoLactation
    {
        /// <summary>
        /// Production rate
        /// </summary>
        public double Production { get; set; }
        /// <summary>
        /// Potential milk production
        /// </summary>
        public double Potential { get; set; }
        /// <summary>
        /// Actual milk production
        /// </summary>
        public double Actual { get; set; }
        /// <summary>
        /// Available milk
        /// </summary>
        public double Available { get; set; }
        /// <summary>
        /// Amount of milk milked
        /// </summary>
        public double Milked { get; set; }
        /// <summary>
        /// Amount of milk suckled
        /// </summary>
        public double Suckled { get; set; }
        /// <summary>
        /// Protein required for lactation
        /// </summary>
        public double Protein { get; set; }

        /// <summary>
        /// Reset milk stores
        /// </summary>
        public void Reset()
        {
            Production = 0;
            Potential = 0;
            Actual = 0;
            Available = 0;
            Milked = 0;
            Suckled = 0;
        }
    }
}
