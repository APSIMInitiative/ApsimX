using Models.Core;

namespace Models.AgPasture
{

    /// <summary>An interface that defines a readonly AgPasture biomass.</summary>
    public interface IAGPBiomass
    {
        /// <summary>Dry matter weight.</summary>
        [Units("kg/ha")]
        double Wt { get; }

        /// <summary>N content of biomass.</summary>
        [Units("kg/ha")]
        double N { get; }

        /// <summary>N concentration.</summary>
        [Units("kg/ha")]
        double NConc { get; }

        /// <summary>Digestibility of biomass.</summary>
        [Units("kg/kg")]
        double Digestibility { get; }

        /// <summary>Average metabolisable energy concentration of standing herbage (MJ/kgDM).</summary>
        [Units("MJ/kg")]
        double ME { get; }
    }
}
