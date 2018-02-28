// ----------------------------------------------------------------------
// <copyright file="InternodeDemandFunction.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Functions.DemandFunctions
{
    using Models.Core;
    using Models.PMF.Struct;
    using System;

    /// <summary>
    /// # [Name]
    /// Calculate internode demand
    /// </summary>
    [Serializable]
    [Description("Internode demand is calculated fromm the product of change in node number, stem population and internode weight.")]
    public class InternodeDemandFunction : BaseFunction
    {
        /// <summary>The value being returned</summary>
        private double[] returnValue = new double[1];

        /// <summary>The inter node wt</summary>
        [Link]
        public IFunction InterNodeWt = null;

        /// <summary>The structure</summary>
        [Link]
        Structure Structure = null;

        /// <summary>Gets the value.</summary>
        public override double[] Values() 
        {
            returnValue[0] = Structure.DeltaTipNumber * Structure.TotalStemPopn * InterNodeWt.Value();
            return returnValue;
        }
    }
}   
