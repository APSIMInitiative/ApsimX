using Models.Soils.Arbitrator;
using System.Collections.Generic;

namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Root interface
    /// </summary>
    public interface IRoot : IOrgan
    {
        /// <summary>Root length density.</summary>
        double[] LengthDensity { get; }

        /// <summary>Root length density modifier due to damage.</summary>
        double RootLengthDensityModifierDueToDamage { get; set; }

        /// <summary>Do the water uptake.</summary>
        /// <param name="Amount">The amount.</param>
        /// <param name="zoneName">Zone name to do water uptake in</param>
        void DoWaterUptake(double[] Amount, string zoneName);

        /// <summary>Do the Nitrogen uptake.</summary>
        /// <param name="zonesFromSoilArbitrator">List of zones from soil arbitrator</param>
        void DoNitrogenUptake(List<ZoneWaterAndN> zonesFromSoilArbitrator);
    }
}
