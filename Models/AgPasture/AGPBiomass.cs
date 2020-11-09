namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System;

    /// <summary>AgPasture class for holding a biomass weight, N content and digestibility.</summary>
    [Serializable]
    public class AGPBiomass : IAGPBiomass
    {
        /// <summary>Dry matter weight.</summary>
        [Units("kg/ha")]
        public double Wt { get; set; }

        /// <summary>N content of biomass.</summary>
        [Units("kg/ha")]
        public double N { get; set; }

        /// <summary>N concentration.</summary>
        [Units("kg/ha")]
        public double NConc { get { return MathUtilities.Divide(N, Wt, 0.0); } }

        /// <summary>Digestibility of biomass.</summary>
        [Units("kg/kg")]
        public double Digestibility { get; set; }

        /// <summary>Average metabolisable energy concentration of standing herbage (MJ/kgDM).</summary>
        [Units("MJ/kg")]
        public double ME { get { return PastureSpecies.PotentialMEOfHerbage * Digestibility; } }
    }
}
