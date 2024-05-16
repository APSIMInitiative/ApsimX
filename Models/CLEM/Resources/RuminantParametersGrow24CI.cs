using DocumentFormat.OpenXml.Presentation;
using Models.CLEM.Interfaces;
using Models.Core;
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
    [Description("RuminantActivityGrow24 (CI - intake parameters)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow24CI.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrow24CI : CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// Relative size scalar (SCA CI1) [Breed] - Growth
        /// </summary>
        [Description("Relative size scalar [CI1]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.025)]
        public double RelativeSizeScalar_CI1 { get; set; }

        /// <summary>
        /// Relative size quadratic (SCA CI2) [Breed] - Growth
        /// </summary>
        [Description("Relative size quadratic [CI2]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1.7)]
        public double RelativeSizeQuadratic_CI2 { get; set; }

        /// <summary>
        /// Rumen Development Curvature (SCA CI3) [Breed] - Growth
        /// </summary>
        [Description("Rumen Development Curvature [CI3]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.22)]
        public double RumenDevelopmentCurvature_CI3 { get; set; }

        /// <summary>
        /// Rumen Development Age (SCA CI4)
        /// </summary>
        [Description("Rumen Development Age [CI4]")]
        [Required, GreaterThanValue(0)]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(60)]
        public double RumenDevelopmentAge_CI4 { get; set; }

        ///// <summary>
        ///// High temperature effect (SCA CI5)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("High temperature effect [CI5]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.02)]
        //public double HighTemperatureEffect_CI5 { get; set; }

        ///// <summary>
        ///// Maximum temperature threshold (SCA CI6)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Maximum temperature threshold [CI6]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(25.0)]
        //public double MaxTemperatureThreshold_CI6 { get; set; }

        ///// <summary>
        ///// Minimum temperature threshold (SCA CI7)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Minimum temperature threshold [CI7]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(22.0)]
        //public double MinTemperatureThreshold_CI7 { get; set; }

        /// <summary>
        /// Peak lactation intake day (SCA CI8)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Peak lactation intake day [CI8]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(62)]
        public double PeakLactationIntakeDay_CI8 { get; set; }

        /// <summary>
        /// Lactation response curvature (SCA CI9)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Lactation response curvature [CI9]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1.7)]
        public double LactationResponseCurvature_CI9 { get; set; }

        /// <summary>
        /// Effect of levels of milk prodiction on intake -  Dairy cows  (SCA CI10)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Effect of levels of milk prodiction on intake [CI10]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.6)]
        public double EffectLevelsMilkProdOnIntake_CI10 { get; set; }

        /// <summary>
        /// Basal milk relative to SRW - Dairy cows  (SCA CI11)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Basal milk relative to SRW [CI11]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.05)]
        public double BasalMilkRelSRW_CI11 { get; set; }

        /// <summary>
        /// Lactation Condition Loss Adjustment (SCA CI12)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Lactation Condition Loss Adjustment [CI12]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.15)]
        public double LactationConditionLossAdjustment_CI12 { get; set; }

        /// <summary>
        /// Lactation Condition Loss Threshold (SCA CI13)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Lactation Condition Loss Threshold [CI13]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.005)]
        public double LactationConditionLossThreshold_CI13 { get; set; }

        /// <summary>
        /// Lactation condition loss threshold decay (SCA CI14)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Lactation condition loss threshold decay [CI14]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.002)]
        public double LactationConditionLossThresholdDecay_CI14 { get; set; }

        /// <summary>
        /// Condition at parturition adjustment (SCA CI15)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Condition at parturition adjustment [CI15]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.5)]
        public double ConditionAtParturitionAdjustment_CI15 { get; set; }

        // CI16 EMPTY

        ///// <summary>
        ///// Low temperature effect (SCA CI17)
        ///// </summary>
        //[Description("Low temperature effect [CI17]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.01)]
        //public double LowTemperatureEffect_CI17 { get; set; }

        ///// <summary>
        ///// Rainfall scalar (SCA CI18)
        ///// </summary>
        //[Description("Rainfall scalar [CI18]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(20.0)]
        //public double RainfallScalar_CI18 { get; set; }

        /// <summary>
        /// Peak lactation intake level (SCA CI19)
        /// </summary>
        [Category("Farm", "Lactation")]
        [Description("Peak lactation intake level [CI19]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(new[] { 0.416, 0.416 })]
        public double[] PeakLactationIntakeLevel_CI19 { get; set; }

        /// <summary>
        /// Relative condition effect (SCA CI20)
        /// </summary>
        [Category("Farm", "Lactation")]
        [Description("Relative condition effect [CI20]")]
        [Required, GreaterThanValue(1)]
        [System.ComponentModel.DefaultValue(1.5)]
        public double RelativeConditionEffect_CI20 { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrow24CI()
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
            RuminantParametersGrow24CI clonedParameters = new()
            {
                RelativeSizeScalar_CI1 = RelativeSizeScalar_CI1,
                RelativeSizeQuadratic_CI2 = RelativeSizeQuadratic_CI2,
                RumenDevelopmentCurvature_CI3 = RumenDevelopmentCurvature_CI3,
                RumenDevelopmentAge_CI4 = RumenDevelopmentAge_CI4,
                PeakLactationIntakeDay_CI8 = PeakLactationIntakeDay_CI8,
                LactationResponseCurvature_CI9 = LactationResponseCurvature_CI9,
                EffectLevelsMilkProdOnIntake_CI10 = EffectLevelsMilkProdOnIntake_CI10,
                BasalMilkRelSRW_CI11 = BasalMilkRelSRW_CI11,
                LactationConditionLossAdjustment_CI12 = LactationConditionLossAdjustment_CI12,
                LactationConditionLossThreshold_CI13 = LactationConditionLossThreshold_CI13,
                LactationConditionLossThresholdDecay_CI14 = LactationConditionLossThresholdDecay_CI14,
                ConditionAtParturitionAdjustment_CI15 = ConditionAtParturitionAdjustment_CI15,
                PeakLactationIntakeLevel_CI19 = PeakLactationIntakeLevel_CI19.Clone() as double[],
                RelativeConditionEffect_CI20 = RelativeConditionEffect_CI20,
            };
            return clonedParameters;
        }
    }
}
