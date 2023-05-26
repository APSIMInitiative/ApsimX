namespace Models.PMF.Interfaces
{
    /// <summary>
    /// An interface for an apex model.
    /// </summary>
    public interface IApex
    {
        /// <summary>Total apex number in plant.</summary>
        double Number { get; set; }

        /// <summary>Total apex number in plant.</summary>
        double[] GroupSize { get; }

        /// <summary>Total apex number in plant.</summary>
        double[] GroupAge { get; }

        /// <summary>
        /// Calculate apex data during leaf tip appearance
        /// </summary>
        /// <param name="population">Current plant population</param>
        /// <param name="totalStemPopn">Current total stem populatin</param>
        double LeafTipAppearance(double population, double totalStemPopn);

        /// <summary>
        /// Calculate apex data during leaf appearance
        /// </summary>
        /// <param name="population">Current plant population</param>
        /// <param name="totalStemPopn">Current total stem populatin</param>
        /// <returns>Cohort population.</returns>
        double Appearance(double population, double totalStemPopn);

        /// <summary>
        /// Reset the apex instance
        /// </summary>
        void Reset();
    }
}
