using System;
using Models.PMF.Interfaces;

namespace Models.PMF.Struct
{

    /// <summary>
    /// Calculate cohort population using stem population.
    /// </summary>
    [Serializable]
    public class ApexStandard : ApexBase, IApex
    {
        /// <summary>
        /// Calculate cohort population at leaf appearance.
        /// </summary>
        /// <param name="population"></param>
        /// <param name="totalStemPopn"></param>
        /// <returns></returns>
        public override double Appearance(double population, double totalStemPopn)
        {
            return totalStemPopn;
        }

        /// <summary>
        /// Calculate cohort population at leaf tip appearance.
        /// </summary>
        /// <param name="population"></param>
        /// <param name="totalStemPopn"></param>
        /// <returns></returns>
        public override double LeafTipAppearance(double population, double totalStemPopn)
        {
            return totalStemPopn;
        }
    }
}
