using DocumentFormat.OpenXml.Spreadsheet;
using Models.CLEM.Groupings;
using Models.Core;
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
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("This model provides all parameters specific to RuminantActivityGrowth (SCA Version)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantActivityGrowSCA.htm")]
    public class RuminantParametersBreed: CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Advanced conception parameters if present
        /// </summary>
        [JsonIgnore]
        public IConceptionModel ConceptionModel { get; set; }

        /// <summary>
        /// Proportion offspring born male
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0.5)]
        [Category("Advanced", "Breeding")]
        [Description("Proportion of offspring male")]
        [Required, Proportion]
        public double ProportionOffspringMale { get; set; }
        
        /// <summary>
        /// Inter-parturition interval intercept of PW (months)
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Inter-parturition interval intercept of PW (months)")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(10.847)]
        public double InterParturitionIntervalIntercept { get; set; }
        
        /// <summary>
        /// Inter-parturition interval coefficient of PW (months)
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Inter-parturition interval coefficient of PW (months)")]
        [Required]
        [System.ComponentModel.DefaultValue(-0.7994)]
        public double InterParturitionIntervalCoefficient { get; set; }
        
        /// <summary>
        /// Minimum age for 1st mating
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Minimum age for 1st mating")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier MinimumAge1stMating { get; set; } = new int[] { 24, 0 };

        /// <summary>
        /// Minimum size for 1st mating, proportion of SRW
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Minimum size for 1st mating, proportion of SRW")]
        [Required, Proportion]
        [System.ComponentModel.DefaultValue(0.6)]
        public double MinimumSize1stMating { get; set; }
        
        /// <summary>
        /// Minimum number of days between last birth and conception
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Minimum number of days between last birth and conception")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(40)]
        public double MinimumDaysBirthToConception { get; set; }
        
        /// <summary>
        /// Rate at which multiple births are concieved (twins, triplets, ...)
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Rate at which multiple births occur (twins,triplets,...")]
        [Proportion]
        public double[] MultipleBirthRate { get; set; }
        
        /// <summary>
        /// Proportion of SRW for zero calving/lambing rate
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Proportion of SRW required before conception possible (min size for mating)")]
        [Required, Proportion]
        public double CriticalCowWeight { get; set; }

        /// <summary>
        /// Maximum number of matings per male per day
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Maximum number of matings per male per day")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(30)]
        public double MaximumMaleMatingsPerDay { get; set; }
        
        /// <summary>
        /// Prenatal mortality rate
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Mortality rate from conception to birth (proportion)")]
        [Required, Proportion]
        [System.ComponentModel.DefaultValue(0.079)]
        public double PrenatalMortality { get; set; }

        /// <summary>
        /// Proportion of wet mother's with no offspring accepting orphan
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Proportion suitable fmeales accpeting orphan")]
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Required, Proportion]
        public double ProportionAcceptingSurrogate { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersBreed()
        {
            this.SetDefaults();
        }

        #region validation

        /// <summary>
        /// Model Validation
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Parent is RuminantType parent)
            {
                if (parent.Parameters.General.BirthScalar.Length != MultipleBirthRate.Length - 1)
                {
                    string[] memberNames = new string[] { "RuminantType.BirthScalar" };
                    results.Add(new ValidationResult($"The number of [BirthScalar] values [{parent.Parameters.General.BirthScalar.Length}] must must be one more than the number of [MultipleBirthRate] values [{parent.Parameters.Breeding.MultipleBirthRate.Length}]. Birth rate scalars represent the size at birth relative to female SRW with one value (default) required for singlets and an additional value for each rate provided in [MultipleBirthRate] representing twins, triplets, quadrulpets etc where required.", memberNames));
                }
            }
            return results;
        }

        #endregion
    }
}
