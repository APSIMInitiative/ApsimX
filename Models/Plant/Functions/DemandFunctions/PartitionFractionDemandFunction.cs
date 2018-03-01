// ----------------------------------------------------------------------
// <copyright file="PartitionFractionDemandFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions.DemandFunctions
{
    using Models.Core;
    using System;

    /// <summary>
    /// # [Name]
    /// This is the Partition Fraction Demand Function which returns the product of its PartitionFraction and the total DM supplied to the arbitrator by all organs
    /// </summary>
    [Serializable]
    [Description("Demand is calculated as a fraction of the total plant supply term.")]
    public class PartitionFractionDemandFunction : BaseFunction
    {
        /// <summary>The partition fraction</summary>
        [Link]
        IFunction PartitionFraction = null;

        /// <summary>The arbitrator</summary>
        [Link]
        OrganArbitrator Arbitrator = null;

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            if (Arbitrator.DM != null)
                return new double[] { Arbitrator.DM.TotalFixationSupply * PartitionFraction.Value() };
            else
                return new double[] { 0 };
        }

    }
}


