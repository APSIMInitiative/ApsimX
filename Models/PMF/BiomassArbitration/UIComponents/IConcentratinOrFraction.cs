namespace Models.PMF
{

    /// <summary>
    /// Interface class for Uptake Methods.
    /// </summary>
    public interface IConcentratinOrFraction

    {
        /// <summary>
        /// Nutrient concnetration or fraction values
        /// </summary>
        NutrientPoolsState ConcentrationsOrFractionss { get; }
    }


}
