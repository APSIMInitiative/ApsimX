

namespace Models.PMF.Functions.DemandFunctions
{
    using System;
    using Models.Core;
    using Models.PMF.Phen;
    using System.Diagnostics;

    /// <summary>
    /// # [Name]
    /// Relative growth rate demand function
    /// </summary>
    [Serializable]
    [Description("This must be renamed DMDemandFunction for the source code to recoginise it!!!!  This function calculates DM demand beyond the start stage as the product of current organ wt (g), relative growth rate and the specified organ number.")]
    public class RelativeGrowthRateDemandFunction : BaseFunction
    {
        /// <summary>The initial wt</summary>
        public double InitialWt = 0;

        /// <summary>The initial stage name</summary>
        public string InitialStageName = "";

        /// <summary>The relative growth rate</summary>
        [Link]
        IFunction RelativeGrowthRate = null;

        /// <summary>The organ number</summary>
        [Link]
        IFunction OrganNumber = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The live</summary>
        [Link]
        Biomass Live = null;

        /// <summary>The start wt</summary>
        double StartWt = 0;

        /// <summary>Gets the value.</summary>
        public override double[] Values()
        {
            if (Phenology.OnDayOf(InitialStageName) && StartWt == 0)
                StartWt = InitialWt;                                   //This is to initiate mass so relative growth rate can kick in
            double CurrentOrganWt = Math.Max(StartWt, Live.Wt / OrganNumber.Value());
            double OrganDemand = CurrentOrganWt * RelativeGrowthRate.Value();
            double returnValue = OrganDemand * OrganNumber.Value();
            Trace.WriteLine("Name: " + Name + " Type: " + GetType().Name + " Value:" + returnValue);
            return new double[] { returnValue };
        }

    }
}


