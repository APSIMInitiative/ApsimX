using Models.Core;
using Models.PMF.Phen;
using System;

namespace Models.PMF.Functions.DemandFunctions
{
    /// <summary>
    /// Potential size demand function
    /// </summary>
    [Serializable]
    [Description("Demand is calculated from the product of potential growth increment, organ number and thermal time.")]
    public class PotentialSizeDemandFunction : Model, IFunction
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
            get { return AccumThermalTime.Value; }
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                if (Phenology.Between(StartStageName, EndStageName))
                    return PotentialGrowthIncrement.Value * OrganNumber.Value * ThermalTime.Value;
                else
                    return 0;
            }
        }

    }
}


