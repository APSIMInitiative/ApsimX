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
    [Description("This must be renamed DMDemandFunction for the source code to recoginise it!!!!.  This function returns the specified proportion of total DM supply.  The organ may not get this proportion if the sum of demands from other organs exceeds DM supply")]
    public class PartitionFractionDemandFunction : Function
    {
        /// <summary>The partition fraction</summary>
        [Link]
        Function PartitionFraction = null;

        /// <summary>The arbitrator</summary>
        [Link]
        OrganArbitrator Arbitrator = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public override double Value
        {
            get
            {
                return Arbitrator.DMSupply * PartitionFraction.Value;
            }
        }

    }
}


