using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;

namespace Models.PMF.Functions.StructureFunctions
{
    /// <summary>
    /// Returns the current InPhase temperature accumulation
    /// </summary>
    [Serializable]
    [Description("Returns the current InPhase temperature accumulation")]
    public class InPhaseTemperatureFunction : Model, IFunction
    {
        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                return Phenology.CurrentPhase.TTinPhase;
            }
        }
    }
}
