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
    /// This stores the parameters relating to RuminantActivityGrow24 for a ruminant Type (CG - Growth parameters)
    /// All default values are provided for Bos taurus cattle with Bos indicus values provided as a comment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersGrow24))]
    [Description("RuminantActivityGrow24 (CP - pregnancy)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow24CP.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrow24CW : CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        ///  (SCA CW1) MJ kg-1
        /// </summary>
        [Category("Farm", "Wool")]
        [Description(" [CW1]")]
        public double StandardFleeceWeight { get; set; } = 4;

        /// <summary>
        ///  (SCA CW1) MJ kg-1
        /// </summary>
        [Category("Breed", "Wool")]
        [Description(" [CW1]")]
        public double EnergyContentCleanWool_CW1 { get; set; } = 24;

        /// <summary>
        ///  (SCA CW2) MJ kg-1
        /// </summary>
        [Category("Breed", "Wool")]
        [Description(" [CW1]")]
        public double BasalCleanWoolGrowth_CW2 { get; set; } = 0.004;

        /// <summary>
        ///  (SCA CW1) MJ kg-1   -- USER
        /// </summary>
        [Category("Farm", "Wool")] 
        [Description(" [CW1]")]
        public double CleanToGreasyCRatio_CW3 { get; set; } = 0.7;

        /// <summary>
        ///  (SCA CW1) MJ kg-1
        /// </summary>
        [Category("Breed", "Wool")]
        [Description(" [CW1]")]
        public double LagFactorForWool_CW4 { get; set; } = 0.04;

        /// <summary>
        ///  (SCA CW1) MJ kg-1 
        /// </summary>
        [Category("Breed", "Wool")]
        [Description(" [CW1]")]
        public double WoolGrowthProportionAtBirth_CW5 { get; set; } = 0.25;

        // CW6 - photoperiod effect NOT INCLUDED

        /// <summary>
        ///  (SCA CW1) MJ kg-1  
        /// </summary>
        [Category("Breed", "Wool")]
        [Description(" [CW1]")]
        public double DPLSLimitationForWoolGrowth_CW7 { get; set; } = 1.35;

        /// <summary>
        ///  (SCA CW1) MJ kg-1   -- kg MJ-1
        /// </summary>
        [Category("Breed", "Wool")]
        [Description(" [CW1]")]
        public double MEILimitationOnWoolGrowth_CW8 { get; set; } = 0.016;

        /// <summary>
        ///  (SCA CW1) MJ kg-1   -- kg MJ-1
        /// </summary>
        [Category("Breed", "Wool")]
        [Description(" [CW1]")]
        public double PregLactationAdjustment_CW9 { get; set; } = 1.0;

        // CW10 and CW11 - not included - Fibre diameter

        /// <summary>
        ///  (SCA CW1) MJ kg-1   -- kg MJ-1
        /// </summary>
        [Category("Breed", "Wool")]
        [Description(" [CW1]")]
        public double AgeFactorExponent_CW12 { get; set; } = 0.025;

        /// <summary>
        /// Create copy of this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            RuminantParametersGrow24CP clonedParameters = new()
            {
                FetalNormWeightParameter_CP2 = FetalNormWeightParameter_CP2,
                FetalNormWeightParameter2_CP3 = FetalNormWeightParameter2_CP3,
                EffectFetalRelativeSizeOnBirthWeight_CP4 = EffectFetalRelativeSizeOnBirthWeight_CP4,
                ConceptusWeightRatio_CP5 = ConceptusWeightRatio_CP5,
                ConceptusWeightParameter_CP6 = ConceptusWeightParameter_CP6,
                ConceptusWeightParameter2_CP7 = ConceptusWeightParameter2_CP7,
                ConceptusEnergyContent_CP8 = ConceptusEnergyContent_CP8,
                ConceptusEnergyParameter_CP9 = ConceptusEnergyParameter_CP9,
                ConceptusEnergyParameter2_CP10 = ConceptusEnergyParameter2_CP10,
                ConceptusProteinPercent_CP11 = ConceptusProteinPercent_CP11,
                ConceptusProteinParameter_CP12 = ConceptusProteinParameter_CP12,
                ConceptusProteinParameter2_CP13 = ConceptusProteinParameter2_CP13,
                FetalGrowthPoorCondition_CP14 = FetalGrowthPoorCondition_CP14
            };
            return clonedParameters;
        }
    }
}
