using Models.PMF.Interfaces;
using Models.PMF.Organs;

namespace Models.Interfaces
{
    /// <summary>
    /// Interface for root shape functions
    /// </summary>
    public interface IRootShape
    {
        /// <summary>Relatives the allocation.</summary>
        /// <param name="zone">The ZoneState.</param>
        void CalcRootProportionInLayers(ZoneState zone);

    }
}