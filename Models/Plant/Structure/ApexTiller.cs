using System;
using Models.PMF.Interfaces;
using Models.Core;

namespace Models.PMF.Struct
{
    /// <summary>
    /// # [Name]
    /// Calculate cohort populations using apex number
    /// </summary>
    [Serializable]
    public class ApexTiller : Model, IApex
    {
        /// <summary>
        /// Calculate cohort population at leaf appearance.
        /// </summary>
        /// <param name="apexNumber"></param>
        /// <param name="population"></param>
        /// <param name="totalStemPopn"></param>
        /// <returns></returns>
        public double Appearance(double apexNumber, double population, double totalStemPopn)
        {
            return apexNumber * population;
        }

        /// <summary>
        /// Calculate cohort population at leaf tip appearance.
        /// </summary>
        /// <param name="apexNumber"></param>
        /// <param name="population"></param>
        /// <param name="totalStemPopn"></param>
        /// <returns></returns>
        public double LeafTipAppearance(double apexNumber, double population, double totalStemPopn)
        {
            return apexNumber * population;
        }
    }
}