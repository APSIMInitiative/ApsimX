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
    public class RuminantInfoOutput
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

        /// <summary>
        /// Nitrogen balance
        /// </summary>
        public double NitrogenBalance { get; set; }

        /// <summary>
        /// Nitrogen excreted
        /// </summary>
        public double NitrogenExcreted { get { return NitrogenUrine + NitrogenFaecal; } }

        /// <summary>
        /// Reset all stores
        /// </summary>
        public void Reset()
        {
            Methane = 0;
            NitrogenUrine = 0;
            NitrogenFaecal = 0;
            Manure = 0;
        }
    }
}
