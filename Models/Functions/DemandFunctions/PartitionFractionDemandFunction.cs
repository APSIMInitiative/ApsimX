using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF;

namespace Models.Functions.DemandFunctions
{
    /// <summary>
    /// # [Name]
    /// This is the Partition Fraction Demand Function which returns the product of its PartitionFraction and the total DM supplied to the arbitrator by all organs.
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
        public double Value(int arrayIndex = -1)
        {
            if (Arbitrator.DM != null)
                return Arbitrator.DM.TotalFixationSupply * PartitionFraction.Value(arrayIndex);
            else
                return 0;
        }

    }
}


