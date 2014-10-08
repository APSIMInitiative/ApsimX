using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns the thermal time accumulation from the current phase in phenology
    /// </summary>
    [Description("Returns the thermal time accumulation from the current phase in phenology")]
    [Serializable]
    public class InPhaseTtFunction : Function
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public override double Value
        {
            get
            {
                return Phenology.CurrentPhase.TTinPhase;
            }
        }
    }
}
