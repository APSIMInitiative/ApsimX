using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Struct;

namespace Models.Functions.DemandFunctions
{
    /// <summary>
    /// # [Name]
    /// Calculate internode demand
    /// </summary>
    [Serializable]
    [Description("Internode demand is calculated fromm the product of change in node number, stem population and internode weight.")]
    public class InternodeDemandFunction : Model, IFunction
    {
        /// <summary>The inter node wt</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction InterNodeWt = null;

        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return Structure.DeltaTipNumber * Structure.TotalStemPopn * InterNodeWt.Value(arrayIndex);
        }
    }
}   
