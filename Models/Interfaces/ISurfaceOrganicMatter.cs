namespace Models.Interfaces
{
    /// <summary>Surface organic matter interface.</summary>
    public interface ISurfaceOrganicMatter
    {
        /// <summary>Fraction of ground covered by all surface OMs</summary>
        double Cover { get; }

        /// <summary>Adds material to the surface organic matter pool.</summary>
        /// <param name="mass">The amount of biomass added (kg/ha).</param>
        /// <param name="N">The amount of N added (kg/ha).</param>
        /// <param name="P">The amount of P added (kg/ha).</param>
        /// <param name="type">Type of the biomass.</param>
        /// <param name="name">Name of the biomass written to summary file</param>
        void Add(double mass, double N, double P, string type, string name);
    }

}
