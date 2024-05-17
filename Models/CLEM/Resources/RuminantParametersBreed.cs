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
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersHolder) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
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
        [System.ComponentModel.DefaultValueAttribute(0.5)]
        [Category("Breed", "Breeding")]
        [Description("Proportion of offspring male")]
        [Required, Proportion]
        public double ProportionOffspringMale { get; set; }
        
        /// <summary>
        /// Inter-parturition interval intercept of PW (months)
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Inter-parturition interval intercept of PW (months)")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(10.847)]
        public double InterParturitionIntervalIntercept { get; set; }
        
        /// <summary>
        /// Inter-parturition interval coefficient of PW (months)
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Inter-parturition interval coefficient of PW (months)")]
        [Required]
        [System.ComponentModel.DefaultValue(-0.7994)]
        public double InterParturitionIntervalCoefficient { get; set; }
        
        /// <summary>
        /// Minimum number of days between last birth and conception
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Minimum number of days between last birth and conception")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(40)]
        public double MinimumDaysBirthToConception { get; set; }
        
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
        [System.ComponentModel.DefaultValue(30)]
        public double MaximumMaleMatingsPerDay { get; set; }
        
        /// <summary>
        /// Prenatal mortality rate
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Mortality rate from conception to birth (proportion)")]
        [Required, Proportion]
        [System.ComponentModel.DefaultValue(0.079)]
        public double PrenatalMortality { get; set; }

        /// <summary>
        /// Allow determination of freemartins for this breed
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Allow freemartins")]
        [Required]
        [System.ComponentModel.DefaultValue(false)]
        public bool AllowFreemartins { get; set; } = false;

        ///// <summary>
        ///// Proportion of wet mother's with no offspring accepting orphan
        ///// </summary>
        //[Category("Farm", "Breeding")]
        //[Description("Proportion suitable females accepting orphan")]
        //[System.ComponentModel.DefaultValueAttribute(0)]
        //[Required, Proportion]
        //public double ProportionAcceptingSurrogate { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersBreeding()
        {
            this.SetDefaults();
        }

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
            };
            return clonedParameters;
        }
    }
}
