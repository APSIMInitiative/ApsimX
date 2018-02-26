using System;
using Models.PMF.Interfaces;
using Models.Core;

namespace Models.PMF.Struct
{
    /// <summary>
    /// # [Name]
    /// Calculate cohort population using stem population.
    /// </summary>
    [Serializable]    
    public class ApexStandard : Model, IApex
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
            return totalStemPopn;
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
            return totalStemPopn;
        }
    }
}
