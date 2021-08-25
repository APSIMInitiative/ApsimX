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

        /// <summary>Root length density modifier due to damage.</summary>
        double RootLengthDensityModifierDueToDamage { get; set; }
    }
    
    /// <summary>
    /// Like Grout but just for the roots
    /// </summary>
    public interface IAmRoout : IOrgan, IWaterNitrogenUptake
    {
        /// <summary>Root length density.</summary>
        double[] LengthDensity { get; }

        /// <summary>Root length density modifier due to damage.</summary>
        double RootLengthDensityModifierDueToDamage { get; set; }
    }
}
