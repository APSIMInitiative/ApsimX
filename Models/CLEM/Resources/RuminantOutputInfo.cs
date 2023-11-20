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
    public class RuminantOutputInfo
    {
        /// <summary>
        /// Enteric methane
        /// </summary>
        public double Methane { get; set; }

        /// <summary>
        /// Nitrogen excreeted in urine
        /// </summary>
        public double NitrogenUrine { get; set; }

        /// <summary>
        /// Nitrogen excreeted in faeces
        /// </summary>
        public double NitrogenFaecal { get; set; }

        /// <summary>
        /// manure
        /// </summary>
        public double Manure { get; set; }

    }
}
