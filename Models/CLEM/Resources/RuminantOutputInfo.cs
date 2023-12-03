using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Stores all ruminant outputs for the time step
    /// </summary>
    [Serializable]
    public class RuminantOutputInfo
    {
        /// <summary>
        /// Enteric methane emitted
        /// </summary>
        public double Methane { get; set; }

        /// <summary>
        /// Nitrogen excreted in urine
        /// </summary>
        public double NitrogenUrine { get; set; }

        /// <summary>
        /// Nitrogen excreted in faeces
        /// </summary>
        public double NitrogenFaecal { get; set; }

        /// <summary>
        /// Manure
        /// </summary>
        public double Manure { get; set; }
    }
}
