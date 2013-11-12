using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.Plant.Functions.DemandFunctions
{
    [Description("This must be renamed DMDemandFunction for the source code to recoginise it!!!!.  This function returns the product of stem population (/m2), Delta leaf number (assuming internodes are expanding at the same rate that leaves are appearing) and the weight internode weight parameter specified")]
    public class InternodeDemandFunction : Function
    {
        [Link]
        Function InterNodeWt = null;

        [Link]
        Structure Structure = null;

        public override double Value
        {
            get
            {
                return Structure.DeltaNodeNumber * Structure.TotalStemPopn * InterNodeWt.Value;
            }
        }
    }
}   
