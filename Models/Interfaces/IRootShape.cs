using Models.PMF.Organs;

namespace Models.Interfaces
{
    /// <summary>
    /// Interface for root shape functions
    /// </summary>
    public interface IRootShape
    {
        /// <summary>Calculates proportion of soil occupied by root in each layer</summary>
        /// <param name="zone">The ZoneState.</param>
        void CalcRootProportionInLayers(ZoneState zone);

    }
}