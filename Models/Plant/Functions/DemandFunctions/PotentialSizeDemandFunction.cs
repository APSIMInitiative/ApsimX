using Models.Core;
using Models.PMF.Phen;
using System;

namespace Models.PMF.Functions.DemandFunctions
{
    [Serializable]
    [Description("This must be renamed DMDemandFunction for the source code to recoginise it!!!!.  This function calculates DM demand between the start and end stages as the product of potential growth rate (g/oCd/organ), daily thermal time and the specified organ number. It returns the product of this potential rate and any childern so if other stress multipliers are required they can be constructed with generic functions.  Stress factors are optional")]
    public class PotentialSizeDemandFunction : Function
    {
        public string StartStageName = "";

        public string EndStageName = "";

        [Link]
        Function PotentialGrowthIncrement = null;

        [Link] 
        Function OrganNumber = null;

        [Link]
        Phenology Phenology = null;

        [Link]
        Function ThermalTime = null;

        [Link]
        Function AccumThermalTime = null;

        [Units("oCd")]
        public double AccumulatedThermalTime //FIXME.  This is not used in Code, check is needed
        {
            get { return AccumThermalTime.Value; }
        }

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


