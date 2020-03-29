namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;

    /// <summary>AgPasture class for holding a biomass weight, N content and digestibility.</summary>
    public class AGPBiomass
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
    }
}
