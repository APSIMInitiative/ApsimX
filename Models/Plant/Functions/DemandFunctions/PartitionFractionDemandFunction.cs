using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions.DemandFunctions
{
    [Description("This must be renamed DMDemandFunction for the source code to recoginise it!!!!.  This function returns the specified proportion of total DM supply.  The organ may not get this proportion if the sum of demands from other organs exceeds DM supply")]
    public class PartitionFractionDemandFunction : Function
    {
        public Function PartitionFraction { get; set; }

        [Link]
        Arbitrator Arbitrator = null;

        public override double FunctionValue
        {
            get
            {
                return Arbitrator.DMSupply * PartitionFraction.FunctionValue;
            }
        }

    }
}


