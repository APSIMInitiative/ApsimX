using System;

namespace Models.GrazPlan
{

    /// <summary>
    /// AnimalStateInfo type. Information required to reset the state in the case of RDP insufficiency                                                                
    /// </summary>
    [Serializable]
    public struct AnimalStateInfo
    {
        /// <summary>
        /// Base weight without wool
        /// </summary>
        public double BaseWeight;

        /// <summary>
        /// Weight of wool
        /// </summary>
        public double WoolWt;

        /// <summary>
        /// Wool microns
        /// </summary>
        public double WoolMicron;

        /// <summary>
        /// Depth of coat
        /// </summary>
        public double CoatDepth;

        /// <summary>
        /// Foetal weight
        /// </summary>
        public double FoetalWt;

        /// <summary>
        /// Lactation adjustment
        /// </summary>
        public double LactAdjust;

        /// <summary>
        /// Lactation ratio
        /// </summary>
        public double LactRatio;

        /// <summary>
        /// Phosphorous value
        /// </summary>
        public double BasePhos;

        /// <summary>
        /// Sulphur value
        /// </summary>
        public double BaseSulf;
    }
}
