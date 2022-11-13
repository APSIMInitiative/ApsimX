namespace Models.DCAPST.Interfaces
{
    /// <summary>
    /// Represents a model that simulates incoming solar radiation
    /// </summary>
    public interface ISolarRadiation
    {
        /// <summary>
        /// The total solar radiation
        /// </summary>
        double Total { get; }

        /// <summary>
        /// The direct component of solar radiation
        /// </summary>
        double Direct { get; }

        /// <summary>
        /// The diffuse component of solar radiation
        /// </summary>
        double Diffuse { get; }

        /// <summary>
        /// Direct radiation expressed in terms of photon count
        /// </summary>
        double DirectPAR { get; }

        /// <summary>
        /// Diffuse radiation expressed in terms of photon count
        /// </summary>
        double DiffusePAR { get; }

        /// <summary>
        /// Sets the radiation values based on the provided time (0.0 to 24.0)
        /// </summary>
        void UpdateRadiationValues(double time);
    }
}
