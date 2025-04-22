namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Root interface
    /// </summary>
    public interface IRoot : IOrgan, IWaterNitrogenUptake
    {
        /// <summary>Rooting depth.</summary>
        double Depth { get; }

        /// <summary>Root length density.</summary>
        double[] LengthDensity { get; }

        /// <summary>Daily soil water uptake from each soil layer.</summary>
        double[] SWUptakeLayered { get; }

        /// <summary>Daily nitrogen uptake from each soil layer.</summary>
        double[] NUptakeLayered { get; }

        /// <summary>Root length density modifier due to damage.</summary>
        double RootLengthDensityModifierDueToDamage { get; set; }
    }
}
