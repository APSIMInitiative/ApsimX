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
    /// This stores the parameters relating to mortality implemented in original RuminantActivityGrow now in RuminantMortalityGroup
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("This model provides parameters for the original model ruminant mortality")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrowMortality.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrowMortality : CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// Mortality rate coefficient
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Mortality rate coefficient")]
        [Required, GreaterThanValue(0)]
        public double MortalityCoefficient { get; set; } = 2.5;

        /// <summary>
        /// Mortality rate intercept
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Mortality rate intercept")]
        [Required, GreaterThanValue(0)]
        public double MortalityIntercept { get; set; } = 0.05;

        /// <summary>
        /// Mortality rate exponent
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Mortality rate exponent")]
        [Required, GreaterThanValue(0)]
        public double MortalityExponent { get; set; } = 3.0;

        /// <summary>
        /// Juvenile mortality rate coefficient
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Juvenile mortality rate coefficient")]
        [Required, GreaterThanValue(0)]
        public double JuvenileMortalityCoefficient { get; set; } = 3.0;

        /// <summary>
        /// Juvenile mortality rate maximum
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Juvenile mortality rate maximum")]
        [Required, Proportion]
        public double JuvenileMortalityMaximum { get; set; } = 0.2;

        /// <summary>
        /// Juvenile mortality rate exponent
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Juvenile mortality rate exponent")]
        [Required]
        public double JuvenileMortalityExponent { get; set; } = 1.8;

        /// <summary>
        /// Create copy of this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            RuminantParametersGrowMortality clonedParameters = new()
            {
                MortalityCoefficient = MortalityCoefficient,
                MortalityExponent = MortalityExponent,
                MortalityIntercept = MortalityIntercept,
                JuvenileMortalityCoefficient = JuvenileMortalityCoefficient,
                JuvenileMortalityExponent = JuvenileMortalityExponent,
                JuvenileMortalityMaximum = JuvenileMortalityMaximum,
            };
            return clonedParameters;
        }
    }
}
