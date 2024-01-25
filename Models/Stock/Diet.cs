using System;

namespace Models.GrazPlan
{

    /// <summary>
    /// Record containing the different sources from which an animal acquires energy, protein etc                                
    /// </summary>
    [Serializable]
    public struct Diet
    {
        /// <summary>
        /// Herbage value
        /// </summary>
        public double Herbage;

        /// <summary>
        /// Supplement value
        /// </summary>
        public double Supp;

        /// <summary>
        /// Milk value
        /// </summary>
        public double Milk;

        /// <summary>
        /// "Solid" is herbage and supplement taken together
        /// </summary>
        public double Solid;

        /// <summary>
        /// Total value
        /// </summary>
        public double Total;
    }
}
