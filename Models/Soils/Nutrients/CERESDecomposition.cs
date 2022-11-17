using Models.Core;
using System;

namespace Models.Functions
{
    /// <summary>A class that calculates CERES decomposition.</summary>
    [Serializable]
    public class CERESDecomposition : Model, IFunction
    {
       [Link(ByName = true, Type = LinkType.Child)]
        private IFunction TF = null;
        
        [Link(ByName = true)]
        private IFunction WF = null;

        [Link(ByName = true, Type = LinkType.Child)]
        private IFunction CNRF = null;

        /// <summary>The potential rate of decomposition</summary>
        [Description("The potential rate of decomposition")]
        public double PotentialRate { get; set; } = 0.0095;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            return PotentialRate * TF.Value(arrayIndex) * WF.Value(arrayIndex) * CNRF.Value(arrayIndex);
        }
    }
}