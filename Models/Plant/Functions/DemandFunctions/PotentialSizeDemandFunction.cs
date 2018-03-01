// ----------------------------------------------------------------------
// <copyright file="PotentialSizeDemandFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions.DemandFunctions
{
    using Models.Core;
    using Models.PMF.Phen;
    using System;

    /// <summary>
    /// # [Name]
    /// Potential size demand function
    /// </summary>
    [Serializable]
    [Description("Demand is calculated from the product of potential growth increment, organ number and thermal time.")]
    public class PotentialSizeDemandFunction : BaseFunction
    {
        /// <summary>The start stage name</summary>
        public string StartStageName = "";

        /// <summary>The end stage name</summary>
        public string EndStageName = "";

        /// <summary>The potential growth increment</summary>
        [Link]
        IFunction PotentialGrowthIncrement = null;

        /// <summary>The organ number</summary>
        [Link]
        IFunction OrganNumber = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The thermal time</summary>
        [Link]
        IFunction ThermalTime = null;

        /// <summary>The accum thermal time</summary>
        [Link]
        IFunction AccumThermalTime = null;

        /// <summary>Gets the accumulated thermal time.</summary>
        /// <value>The accumulated thermal time.</value>
        [Units("oCd")]
        public double AccumulatedThermalTime //FIXME.  This is not used in Code, check is needed
        {
            get { return AccumThermalTime.Value(); }
        }

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            if (Phenology.Between(StartStageName, EndStageName))
                return new double[] { PotentialGrowthIncrement.Value() * OrganNumber.Value() * ThermalTime.Value() };
            else
                return new double[] { 0 };
        }

    }
}


