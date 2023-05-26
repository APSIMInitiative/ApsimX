using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Advanced ruminant conception for first conception less than 12 months, 12-24 months, 2nd calf and 3+ calf
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("Advanced ruminant conception for first pregnancy less than 12 months, 12-24 months, 24 months, 2nd calf and 3+ calf")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantAdvancedConception.htm")]
    public class RuminantConceptionAdvanced : CLEMModel, IConceptionModel
    {

        /// <summary>
        /// Conception rate coefficient of breeder
        /// </summary>
        [Description("Conception rate coefficient of breeder PW (<12 mnth, 24 mth, 2nd calf, 3rd+ calf)")]
        [Required, ArrayItemCount(4)]
        public double[] ConceptionRateCoefficent { get; set; }
        /// <summary>
        /// Conception rate intercept of breeder
        /// </summary>
        [Description("Conception rate intercept of breeder PW (<12 mnth, 24 mth, 2nd calf, 3rd+ calf)")]
        [Required, ArrayItemCount(4)]
        public double[] ConceptionRateIntercept { get; set; }
        /// <summary>
        /// Conception rate asymptote of breeder
        /// </summary>
        [Description("Conception rate asymptote (<12 mnth, 24 mth, 2nd calf, 3rd+ calf)")]
        [Required, ArrayItemCount(4)]
        public double[] ConceptionRateAsymptote { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public RuminantConceptionAdvanced()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <summary>
        /// Calculate conception rate for a female
        /// </summary>
        /// <param name="female">Female to calculate conception rate for</param>
        /// <returns></returns>
        public double ConceptionRate(RuminantFemale female)
        {
            double rate = 0;

            if (female.StandardReferenceWeight > 0)
            {
                // generalised curve
                switch (female.NumberOfBirths)
                {
                    case 0:
                        // first mating
                        //if (female.BreedParams.MinimumAge1stMating >= 24)
                        if (female.Age >= 24)
                            // 1st mated at 24 months or older
                            rate = ConceptionRateAsymptote[1] / (1 + Math.Exp(ConceptionRateCoefficent[1] * female.Weight / female.StandardReferenceWeight + ConceptionRateIntercept[1]));
                        //else if (female.BreedParams.MinimumAge1stMating >= 12)
                        else if (female.Age >= 12)
                        {
                            // 1st mated between 12 and 24 months
                            double rate24 = ConceptionRateAsymptote[1] / (1 + Math.Exp(ConceptionRateCoefficent[1] * female.Weight / female.StandardReferenceWeight + ConceptionRateIntercept[1]));
                            double rate12 = ConceptionRateAsymptote[0] / (1 + Math.Exp(ConceptionRateCoefficent[0] * female.Weight / female.StandardReferenceWeight + ConceptionRateIntercept[0]));
                            // interpolate, not just average
                            double propOfYear = (female.Age - 12) / 12;
                            rate = rate12 + ((rate24 - rate12) * propOfYear);
                        }
                        else
                            // first mating < 12 months old
                            rate = ConceptionRateAsymptote[0] / (1 + Math.Exp(ConceptionRateCoefficent[0] * female.Weight / female.StandardReferenceWeight + ConceptionRateIntercept[0]));
                        break;
                    case 1:
                        // second offspring mother
                        rate = ConceptionRateAsymptote[2] / (1 + Math.Exp(ConceptionRateCoefficent[2] * female.Weight / female.StandardReferenceWeight + ConceptionRateIntercept[2]));
                        break;
                    default:
                        // females who have had more than two births (twins should count as one birth)
                        if (female.WeightAtConception > female.BreedParams.CriticalCowWeight * female.StandardReferenceWeight)
                            rate = ConceptionRateAsymptote[3] / (1 + Math.Exp(ConceptionRateCoefficent[3] * female.Weight / female.StandardReferenceWeight + ConceptionRateIntercept[3]));
                        break;
                }
            }
            rate = Math.Max(0, Math.Min(rate, 100));
            return rate / 100;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            return "<div class=\"activityentry\">Conception rates are being calculated for first pregnancy before 12 months, between 12-24 months and after 24 months as well as 2nd calf and 3rd or later calf. </div>";
        }


        #endregion
    }
}
