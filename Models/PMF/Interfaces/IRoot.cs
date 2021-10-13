using Models.Soils.Arbitrator;
using System.Collections.Generic;

namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Root interface
    /// </summary>
    public interface IRoot : IOrgan, IWaterNitrogenUptake
    {
        /// <summary>Root length density.</summary>
        double[] LengthDensity { get; }

        /// <summary>Daily soil water uptake from each soil layer.</summary>
        double[] SWUptake { get; }

        /// <summary>Daily nitrogen uptake from each soil layer.</summary>
        double[] NUptake { get; }

        /// <summary>Root length density modifier due to damage.</summary>
        double RootLengthDensityModifierDueToDamage { get; set; }
    }
}
