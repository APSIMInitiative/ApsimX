using System;

namespace Models.PMF
{
    /// <summary>
    /// Parameters which control how a plant is harvested
    /// </summary>
    [Serializable]
    public class HarvestingParameters : EventArgs
    {
        /// <summary>The plant being sown.</summary>
        public bool RemoveBiomass;
    }
}
