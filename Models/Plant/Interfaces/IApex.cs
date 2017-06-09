namespace Models.PMF.Interfaces
{
    /// <summary>
    /// An interface for an apex model.
    /// </summary>
    public interface IApex
    {
        /// <summary>
        /// Calculate apex data during leaf tip appearance
        /// </summary>
        /// <param name="apexNumber">Current apex number.</param>
        /// <param name="population">Current plant population</param>
        /// <param name="totalStemPopn">Current total stem populatin</param>
        double LeafTipAppearance(double apexNumber, double population, double totalStemPopn);

        /// <summary>
        /// Calculate apex data during leaf appearance
        /// </summary>
        /// <param name="apexNumber">Current apex number.</param>
        /// <param name="population">Current plant population</param>
        /// <param name="totalStemPopn">Current total stem populatin</param>
        /// <returns>Cohort population.</returns>
        double Appearance(double apexNumber, double population, double totalStemPopn);
    }
}
