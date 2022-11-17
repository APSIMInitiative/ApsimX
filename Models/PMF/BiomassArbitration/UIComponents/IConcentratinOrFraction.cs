namespace Models.PMF
{
    using Models.Core;
    using Models.Functions;
    using Models.PMF.Interfaces;
    using Models.PMF.Organs;
    using System;
    using System.Collections.Generic;

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
