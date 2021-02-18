namespace Models.PMF.Struct
{
    using System;
    using Models.Core;
    using Models.PMF.Interfaces;

    /// <summary>
    /// # [Name]
    /// Calculate cohort populations using apex number
    /// </summary>
    [Serializable]
    public class ApexTiller : ApexBase, IApex
    {
        /// <summary>
        /// Calculate cohort population at leaf appearance.
        /// </summary>
        /// <param name="population"></param>
        /// <param name="totalStemPopn"></param>
        /// <returns></returns>
        public override double Appearance(double population, double totalStemPopn)
        {
            return Number * population;
        }

        /// <summary>
        /// Calculate cohort population at leaf tip appearance.
        /// </summary>
        /// <param name="population"></param>
        /// <param name="totalStemPopn"></param>
        /// <returns></returns>
        public override double LeafTipAppearance(double population, double totalStemPopn)
        {
            return Number * population;
        }
    }
}