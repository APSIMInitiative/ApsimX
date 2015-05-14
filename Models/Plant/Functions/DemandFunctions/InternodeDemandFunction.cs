using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;

namespace Models.PMF.Functions.DemandFunctions
{
    /// <summary>
    /// Calculate internode demand
    /// </summary>
    [Serializable]
    [Description("Internode demand is calculated fromm the product of change in node number, stem population and internode weight.")]
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
