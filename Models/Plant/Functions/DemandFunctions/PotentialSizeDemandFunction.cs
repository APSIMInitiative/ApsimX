using Models.Core;
using Models.PMF.Phen;
using System;

namespace Models.PMF.Functions.DemandFunctions
{
    /// <summary>
    /// Potential size demand function
    /// </summary>
    [Serializable]
    [Description("This must be renamed DMDemandFunction for the source code to recoginise it!!!!.  This function calculates DM demand between the start and end stages as the product of potential growth rate (g/oCd/organ), daily thermal time and the specified organ number. It returns the product of this potential rate and any childern so if other stress multipliers are required they can be constructed with generic functions.  Stress factors are optional")]
    public class PotentialSizeDemandFunction : Function
    {
        /// <summary>The start stage name</summary>
        public string StartStageName = "";

        /// <summary>The end stage name</summary>
        public string EndStageName = "";

        /// <summary>The potential growth increment</summary>
        [Link]
        Function PotentialGrowthIncrement = null;

        /// <summary>The organ number</summary>
        [Link] 
        Function OrganNumber = null;

        /// <summary>The phenology</summary>
        [Link]
        Phenology Phenology = null;

        /// <summary>The thermal time</summary>
        [Link]
        Function ThermalTime = null;

        /// <summary>The accum thermal time</summary>
        [Link]
        Function AccumThermalTime = null;

        /// <summary>Gets the accumulated thermal time.</summary>
        /// <value>The accumulated thermal time.</value>
        [Units("oCd")]
        public double AccumulatedThermalTime //FIXME.  This is not used in Code, check is needed
        {
            get { return AccumThermalTime.Value; }
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public override double Value
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


