using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// Class for reporting transaction details in EcologicalIndicatorsCalculated Events
    /// </summary>
    [Serializable]
    public class EcolIndicatorsEventArgs : EventArgs
    {
        /// <summary>
        /// Ecological indicators details
        /// </summary>
        public EcologicalIndicators Indicators { get; set; }
    }
}
