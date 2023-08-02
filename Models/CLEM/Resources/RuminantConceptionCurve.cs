using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// The simplest ruminant conception using a single curve
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("Define ruminant conception using a single curve")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantConceptionCurve.htm")]
    public class RuminantConceptionCurve : CLEMModel, IConceptionModel
    {
        /// <summary>
        /// constructor
        /// </summary>
        public RuminantConceptionCurve()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Conception rate coefficient of breeder
        /// </summary>
        [Description("Conception rate coefficient of breeder PW")]
        [Required]
        public double ConceptionRateCoefficent { get; set; }

        /// <summary>
        /// Conception rate intercept of breeder
        /// </summary>
        [Description("Conception rate intercept of breeder PW")]
        [Required]
        public double ConceptionRateIntercept { get; set; }

        /// <summary>
        /// Conception rate asymptote of breeder
        /// </summary>
        [Description("Conception rate asymptote")]
        [Required]
        public double ConceptionRateAsymptote { get; set; }

        /// <summary>
        /// Calculate conception rate for a female
        /// </summary>
        /// <param name="female">Female to calculate conception rate for</param>
        /// <returns></returns>
        public double ConceptionRate(RuminantFemale female)
        {
            double rate = 0;
            if (female.StandardReferenceWeight > 0)
                rate = ConceptionRateAsymptote / (1 + Math.Exp(ConceptionRateCoefficent * female.Weight / female.StandardReferenceWeight + ConceptionRateIntercept));

            rate = Math.Max(0, Math.Min(rate, 100));
            return rate / 100;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            return "<div class=\"activityentry\">Conception rates are being calculated for all females using the same curve.</div>";
        }

        #endregion
    }
}
