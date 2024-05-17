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
    /// All default values are provided for cattle and Bos indicus breeds where values apply.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersGrow24))]
    [Description("RuminantActivityGrow24 (CP - pregnancy)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow24CP.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrow24) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
    public class RuminantParametersGrow24CP : CLEMModel, ISubParameters, ICloneable
    {
        // CP1 gestation length (see Parameters.General.GestationLength) 

        /// <summary>
        /// Fetal normalised weight parameter (SCA CP2)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Fetal normalised weight parameter [CP2]")]
        [System.ComponentModel.DefaultValue(2.2)]
        public double FetalNormWeightParameter_CP2 { get; set; }

        /// <summary>
        /// Fetal normalised weight parameter #2 (SCA CP3)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Fetal normalised weight parameter 2 [CP3]")]
        [System.ComponentModel.DefaultValue(1.77)]
        public double FetalNormWeightParameter2_CP3 { get; set; }

        /// <summary>
        /// Effect fetal relative size on birth weight (SCA CP4)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Effect fetal relative size on birth weight [CP4]")]
        [System.ComponentModel.DefaultValue(0.33)]
        public double EffectFetalRelativeSizeOnBirthWeight_CP4 { get; set; }

        /// <summary>
        /// Conceptus weight ratio (SCA CP5)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Conceptus weight ratio [CP5]")]
        [System.ComponentModel.DefaultValue(1.8)]
        public double ConceptusWeightRatio_CP5 { get; set; }

        /// <summary>
        /// Conceptus weight parameter (SCA CP6)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Conceptus weight parameter [CP6]")]
        [System.ComponentModel.DefaultValue(2.42)]
        public double ConceptusWeightParameter_CP6 { get; set; }

        /// <summary>
        /// Conceptus weight parameter #2 (SCA CP7)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Conceptus weight parameter 2 [CP7]")]
        [System.ComponentModel.DefaultValue(1.16)]
        public double ConceptusWeightParameter2_CP7 { get; set; }

        /// <summary>
        /// Conceptus energy content (SCA CP8)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Conceptus energy content [CP8]")]
        [System.ComponentModel.DefaultValue(4.11)]
        public double ConceptusEnergyContent_CP8 { get; set; }

        /// <summary>
        /// Conceptus energy parameter (SCA CP9)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Conceptus weight parameter [CP9]")]
        [System.ComponentModel.DefaultValue(343.5)]
        public double ConceptusEnergyParameter_CP9 { get; set; }

        /// <summary>
        /// Conceptus energy parameter #2 (SCA CP10)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Conceptus weight parameter 2 [CP10]")]
        [System.ComponentModel.DefaultValue(0.0164)]
        public double ConceptusEnergyParameter2_CP10 { get; set; }

        /// <summary>
        /// Conceptus protein content (SCA CP11)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Conceptus protein content [CP11]")]
        [System.ComponentModel.DefaultValue(0.134)]
        public double ConceptusProteinContent_CP11 { get; set; }

        /// <summary>
        /// Conceptus protein parameter (SCA CP12)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Conceptus protein parameter [CP12]")]
        [System.ComponentModel.DefaultValue(6.22)]
        public double ConceptusProteinParameter_CP12 { get; set; }

        /// <summary>
        /// Conceptus protein parameter #2 (SCA CP13)
        /// </summary>
        [Category("Breed", "Pregnancy")]
        [Description("Conceptus protein parameter 2 [CP13]")]
        [System.ComponentModel.DefaultValue(0.747)]
        public double ConceptusProteinParameter2_CP13 { get; set; }

        /// <summary>
        /// Fetal growth in poor condition for 1,2,3,... young (SCA CP14)
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Fetal growth in poor condition [CP14]")]
        [Required, MinLength(1)]
        [System.ComponentModel.DefaultValue(new[] { 1.0, 1.15 })]
        public double[] FetalGrowthPoorCondition_CP14 { get; set; }

        // CP15 relative size birth weight (see Parameters.General.BirthScalar) 

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrow24CP()
        {
            this.SetDefaults();
        }

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
                ConceptusProteinContent_CP11 = ConceptusProteinContent_CP11,
                ConceptusProteinParameter_CP12 = ConceptusProteinParameter_CP12,
                ConceptusProteinParameter2_CP13 = ConceptusProteinParameter2_CP13,
                FetalGrowthPoorCondition_CP14 = FetalGrowthPoorCondition_CP14
            };
            return clonedParameters;
        }
    }
}
