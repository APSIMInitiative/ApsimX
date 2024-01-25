namespace Models.PMF
{
    /// <summary>Interface for all biomass objects.</summary>
    public interface IBiomass
    {
        /// <summary>Gets the nitrogen amount.</summary>
        double N { get; }

        /// <summary>Gets the nitrogen concentration.</summary>
        double NConc { get; }

        /// <summary>Gets the mass.</summary>
        double Wt { get; }

        /// <summary>Gets the structural mass.</summary>
        double StructuralWt { get; }

        /// <summary>Gets the structural nitrogen.</summary>
        double StructuralN { get; }

        /// <summary>Gets the storage mass.</summary>
        double StorageWt { get; }

        /// <summary>Gets the storage nitrogen.</summary>
        double StorageN { get; }
    }
}