namespace Models.AgPasture
{
    using Models.Core;

    interface IAGPBiomass
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
