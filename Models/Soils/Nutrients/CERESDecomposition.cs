using Models.Core;
using System;

namespace Models.Functions
{
    /// <summary>A class that calculates CERES decomposition.</summary>
    [Serializable]
    public class CERESDecomposition : Model, IFunction
    {
        /// <summary>The child functions</summary>
        private double potentialRate = 0.0095;

        [Link(ByName = true, Type = LinkType.Child)]
        private IFunction TF = null;
        
        [Link(ByName = true)]
        private IFunction WF = null;

        [Link(ByName = true, Type = LinkType.Child)]
        private IFunction CNRF = null;

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            return potentialRate * TF.Value(arrayIndex) * WF.Value(arrayIndex) * CNRF.Value(arrayIndex);
        }
    }
}