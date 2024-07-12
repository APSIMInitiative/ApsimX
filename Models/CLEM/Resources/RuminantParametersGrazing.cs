using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters relating to RuminantActivityGrowSCA for a ruminant Type
    /// All default values are provided for Bos taurus cattle with Bos indicus values provided as a comment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("This model provides all parameters specific to RuminantActivityGrowth (SCA Version)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrazing.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrazing: CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// Maximum green in diet
        /// </summary>
        [Category("Farm", "Grazing")]
        [Description("Maximum green in diet")]
        [Required, Proportion]
        public double GreenDietMax { get; set; } = 0.98;

        /// <summary>
        /// Shape of curve for diet vs pasture
        /// </summary>
        [Category("Breed", "Grazing")]
        [Description("Shape of curve for diet vs pasture")]
        [Required, GreaterThanValue(0)]
        public double GreenDietCoefficient { get; set; } = 0.15;

        /// <summary>
        /// Proportion green in pasture at zero in diet
        /// was %
        /// </summary>
        [Category("Farm", "Diet")]
        [Description("Proportion green in pasture at zero in diet")]
        [Required, Proportion]
        public double GreenDietZero { get; set; } = 0.04;

        /// <summary>
        /// Coefficient to adjust intake for herbage biomass
        /// </summary>
        [Category("Farm", "Diet")]
        [Description("Coefficient to adjust intake for herbage biomass")]
        [Required, GreaterThanValue(0)]

        public double IntakeCoefficientBiomass { get; set; } = 0.01;

        /// <summary>
        /// Enforce strict feeding limits
        /// </summary>
        [Category("Farm", "Diet")]
        [Description("Enforce strict feeding limits")]
        [Required]
        public bool StrictFeedingLimits { get; set; } = true;

        /// <summary>
        /// Clone of the Grazing Parameters
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            RuminantParametersGrazing clonedParameters = new()
            {
                GreenDietMax = GreenDietMax,
                GreenDietCoefficient = GreenDietCoefficient,
                GreenDietZero = GreenDietZero,
                IntakeCoefficientBiomass = IntakeCoefficientBiomass,
                StrictFeedingLimits = StrictFeedingLimits
            };
            return clonedParameters;
        }
    }
}
