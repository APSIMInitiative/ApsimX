using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions.DemandFunctions
{
    /// <summary>
    /// This must be renamed DMDemandFunction for the source code to recoginise it!!!!.  This function returns the product of stem population (/m2), Delta leaf number (assuming internodes are expanding at the same rate that leaves are appearing) and the weight internode weight parameter specified
    /// </summary>
    [Serializable]
    [Description("This must be renamed DMDemandFunction for the source code to recoginise it!!!!.  This function returns the product of stem population (/m2), Delta leaf number (assuming internodes are expanding at the same rate that leaves are appearing) and the weight internode weight parameter specified")]
    public class InternodeDemandFunction : Model, IFunction
    {
        /// <summary>The inter node wt</summary>
        [Link]
        public IFunction InterNodeWt = null;

        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                return Structure.DeltaNodeNumber * Structure.TotalStemPopn * InterNodeWt.Value;
            }
        }
    }
}   
