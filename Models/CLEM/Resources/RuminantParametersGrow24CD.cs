using DocumentFormat.OpenXml.Presentation;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.DCAPST.Environment;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Intrinsics.X86;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters relating to RuminantActivityGrow24 for a ruminant Type (CD - Death parameters)
    /// All default values are provided for Bos taurus cattle with Bos indicus values provided as a comment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersGrow24))]
    [Description("RuminantActivityGrow24 (CD - death)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow24CD.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrow24CD : CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// Basal mortality rate CD1
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Basal mortality rate")]
        [Required, GreaterThanValue(0)]
        public double BasalMortalityRate_CD1 { get; set; } = 5.53e-5;

        /// <summary>
        /// Effect body condition on mortality # 1 CD2
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Effect body condition on mortality #1")]
        [Required, GreaterThanValue(0)]
        public double EffectBCOnMortality1_CD2 { get; set; } = 0.3;

        /// <summary>
        /// Effect body condition on mortality # 2 CD3
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Effect body condition on mortality #2")]
        [Required, GreaterThanValue(0)]
        public double EffectBCOnMortality2_CD3 { get; set; } = 0.6;

        /// <summary>
        /// Lower bound for pregnancy toximia 
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Lower bound for pregnancy toximia")]
        [Required, GreaterThanEqualValue(0)]
        public double LowerBoundsPregnancyToximia_CD4 { get; set; } = 0.0;

        /// <summary>
        /// Upper bound for pregnancy toximia 
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Upper bound for pregnancy toximia")]
        [Required, GreaterThanEqualValue(0)]
        public double UpperBoundsPregnancyToximia_CD5 { get; set; } = 0.0;

        /// <summary>
        /// Lower bound for pregnancy dystocia
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Lower bound for pregnancy dystocia")]
        [Required, GreaterThanEqualValue(0)]
        public double LowerBoundsPregnancyDystocia_CD6 { get; set; } = 0.0;

        /// <summary>
        /// Upper bound for pregnancy dystocia 
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Upper bound for pregnancy dystocia")]
        [Required, GreaterThanEqualValue(0)]
        public double UpperBoundsPregnancyDystocia_CD7 { get; set; } = 0.0;

        /// <summary>
        /// Critical ratio of fat to EBM content for survival 
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Critical fat to EBM proportion for survival")]
        [Required, GreaterThanValue(0), Proportion]
        public double CriticalFatToEBMForSurvival { get; set; } = 0.05;

        // ToDo: Add dystocia for cattle at some stage.

        // CD8-CD11 Chilling effect - not implemented

        /// <summary>
        /// Relative difference in weight of dying animals
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Relative difference in weight of dying animals")]
        [Required, GreaterThanValue(0)]
        public double RelativeDifferenceWeightDyingIndividuals_CD12 { get; set; } = 0.1;

        /// <summary>
        /// Upper limit for mortality in weaners 
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Upper limit for mortality in weaners")]
        [Required, GreaterThanValue(0)]
        public double UpperLimitForMortalityInWeaners_CD13 { get; set; } = 1e-4; //ToDo find suitable default.. This is about 2x base mortality for now. So 2x mort at weaning dropping to base by 1 year old.

        // CD14 age where indivudal condidered wenaer for mort.

        // CD15 upper age of reduction in mort for weaners. Set in code based on weaner rule (12 months).

        /// <summary>
        /// Create copy of this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            RuminantParametersGrow24CD clonedParameters = new()
            {
                BasalMortalityRate_CD1 = BasalMortalityRate_CD1,
                EffectBCOnMortality1_CD2 = EffectBCOnMortality1_CD2,
                EffectBCOnMortality2_CD3 = EffectBCOnMortality2_CD3,
                LowerBoundsPregnancyToximia_CD4 = LowerBoundsPregnancyToximia_CD4,
                UpperBoundsPregnancyToximia_CD5 = UpperBoundsPregnancyToximia_CD5,
                LowerBoundsPregnancyDystocia_CD6 = LowerBoundsPregnancyDystocia_CD6,
                UpperBoundsPregnancyDystocia_CD7 = UpperBoundsPregnancyDystocia_CD7,
                RelativeDifferenceWeightDyingIndividuals_CD12 = RelativeDifferenceWeightDyingIndividuals_CD12,
                UpperLimitForMortalityInWeaners_CD13 = UpperLimitForMortalityInWeaners_CD13
            };
            return clonedParameters;
        }
    }
}
