using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions.DemandFunctions
{
    /// <summary>
    /// Partition fraction demand function
    /// </summary>
    [Serializable]
    [Description("Demand is calculated as a fraction of the total plant supply term.")]
    public class PartitionFractionDemandFunction : Model, IFunction
    {
        /// <summary>The partition fraction</summary>
        [Link]
        IFunction PartitionFraction = null;

        /// <summary>The arbitrator</summary>
        [Link]
        OrganArbitrator Arbitrator = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                return Arbitrator.DMSupply * PartitionFraction.Value;
            }
        }

    }
}


