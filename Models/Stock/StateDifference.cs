using System;

namespace Models.GrazPlan
{

    /// <summary>
    /// Set of differences between two sub-groups of animals.  Used in the Split  
    /// method of AnimalGroup                                                     
    /// </summary>
    [Serializable]
    public struct DifferenceRecord
    {
        /// <summary>
        /// Standard reference weight
        /// </summary>
        public double StdRefWt;

        /// <summary>
        /// Base weight
        /// </summary>
        public double BaseWeight;

        /// <summary>
        /// Fleece weight
        /// </summary>
        public double FleeceWt;
    }
}
