using DocumentFormat.OpenXml.Spreadsheet;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Models.CLEM.Resources
{ 
    /// <summary>
    /// This stores the parameters relating to RuminantActivityGrowSCA for a ruminant Type
    /// All default values are provided for cattle and Bos indicus breeds where values apply.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("This model provides all parameters specific to RuminantActivityGrowth (SCA Version)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersBreed.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersBreeding: CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// Advanced conception parameters if present
        /// </summary>
        [JsonIgnore]
        public IConceptionModel ConceptionModel { get; set; }

        /// <summary>
        /// Proportion offspring born male
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Proportion of offspring male")]
        [Required, Proportion]
        public double ProportionOffspringMale { get; set; } = 0.5;
        
        /// <summary>
        /// Inter-parturition interval intercept of PW (months)
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Inter-parturition interval intercept of PW (months)")]
        [Required, GreaterThanValue(0)]
        public double InterParturitionIntervalIntercept { get; set; } = 10.847;

        /// <summary>
        /// Inter-parturition interval coefficient of PW (months)
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Inter-parturition interval coefficient of PW (months)")]
        [Required]
        public double InterParturitionIntervalCoefficient { get; set; } = -0.7994;

        /// <summary>
        /// Minimum number of days between last birth and conception
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Minimum number of days between last birth and conception")]
        [Required, GreaterThanValue(0)]
        public double MinimumDaysBirthToConception { get; set; } = 40; 
        
        /// <summary>
        /// Proportion of SRW for zero calving/lambing rate
        /// </summary>
        [Category("Farm", "Breeding")]
        [Description("Proportion of SRW required before conception possible (min size for mating)")]
        [Required, Proportion]
        public double CriticalCowWeight { get; set; }

        /// <summary>
        /// Maximum number of matings per male per day
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Maximum number of matings per male per day")]
        [Required, GreaterThanValue(0)]
        public double MaximumMaleMatingsPerDay { get; set; } = 30;
        
        /// <summary>
        /// Prenatal mortality rate
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Mortality rate from conception to birth (proportion)")]
        [Required, Proportion]
        public double PrenatalMortality { get; set; } = 0.079;

        /// <summary>
        /// Allow determination of freemartins for this breed
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Allow freemartins")]
        [Required]
        public bool AllowFreemartins { get; set; } = false;

        /// <summary>
        /// Probability of conceiving while lactating
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Probability of conception during lactation")]
        [Required, Proportion]
        public double ConceptionDuringLactationProbability { get; set; } = 1.0;

        ///// <summary>
        ///// Proportion of wet mother's with no offspring accepting orphan
        ///// </summary>
        //[Category("Farm", "Breeding")]
        //[Description("Proportion suitable females accepting orphan")]
        //[Required, Proportion]
        //public double ProportionAcceptingSurrogate { get; set; } = 0;

        /// <summary>
        /// Create a clone of this class
        /// </summary>
        /// <returns>A copy of the class</returns>
        public object Clone()
        {
            RuminantParametersBreeding clonedParameters = new()
            {
                AllowFreemartins = AllowFreemartins,
                CriticalCowWeight = CriticalCowWeight,
                InterParturitionIntervalCoefficient = InterParturitionIntervalCoefficient,
                InterParturitionIntervalIntercept = InterParturitionIntervalIntercept,
                MinimumDaysBirthToConception = MinimumDaysBirthToConception,
                MaximumMaleMatingsPerDay = MaximumMaleMatingsPerDay, 
                PrenatalMortality = PrenatalMortality,
                ConceptionDuringLactationProbability = ConceptionDuringLactationProbability,
            };
            return clonedParameters;
        }
    }
}
