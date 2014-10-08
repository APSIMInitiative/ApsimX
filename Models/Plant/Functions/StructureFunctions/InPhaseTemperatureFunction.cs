using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions.StructureFunctions
{
    /// <summary>
    /// Returns the curreent InPhase tempature accumulation
    /// </summary>
    [Serializable]
    [Description("Returns the curreent InPhase tempature accumulation")]
    public class InPhaseTemperatureFunction : Function
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
