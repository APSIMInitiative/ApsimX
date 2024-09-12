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
        /// Standard Fleece Weight
        /// </summary>
        [Category("Farm", "Wool")]
        [Description("Standard Fleece Weight")]
        public double StandardFleeceWeight { get; set; } = 4;

        /// <summary>
        /// Energy Content Clean Wool (SCA CW1) MJ kg-1
        /// </summary>
        [Category("Breed", "Wool")]
        [Description("Energy Content Clean Wool [CW1]")]
        public double EnergyContentCleanWool_CW1 { get; set; } = 24;

        /// <summary>
        /// Basal Clean Wool Growth (SCA CW2)
        /// </summary>
        [Category("Breed", "Wool")]
        [Description("Basal Clean Wool Growth [CW2]")]
        public double BasalCleanWoolGrowth_CW2 { get; set; } = 0.004;

        /// <summary>
        /// Clean To Greasy C Ratio (SCA CW3)   -- USER
        /// </summary>
        [Category("Farm", "Wool")] 
        [Description("Clean To Greasy C Ratio [CW3]")]
        public double CleanToGreasyCRatio_CW3 { get; set; } = 0.7;

        /// <summary>
        /// Lag Factor For Wool (SCA CW4)
        /// </summary>
        [Category("Breed", "Wool")]
        [Description("Lag Factor For Wool [CW4]")]
        public double LagFactorForWool_CW4 { get; set; } = 0.04;

        /// <summary>
        /// Wool Growth Proportion At Birth (SCA CW5)
        /// </summary>
        [Category("Breed", "Wool")]
        [Description("Wool Growth Proportion At Birth [CW5]")]
        public double WoolGrowthProportionAtBirth_CW5 { get; set; } = 0.25;

        // CW6 - photoperiod effect NOT INCLUDED

        /// <summary>
        /// DPLS Limitation For Wool Growth (SCA CW7)  
        /// </summary>
        [Category("Breed", "Wool")]
        [Description("DPLS Limitation For Wool Growth [CW7]")]
        public double DPLSLimitationForWoolGrowth_CW7 { get; set; } = 1.35;

        /// <summary>
        /// MEI Limitation On Wool Growth (SCA CW8)
        /// </summary>
        [Category("Breed", "Wool")]
        [Description("MEI Limitation On Wool Growth [CW8]")]
        public double MEILimitationOnWoolGrowth_CW8 { get; set; } = 0.016;

        /// <summary>
        /// Pregnancy Lactation Adjustment (SCA CW9)
        /// </summary>
        [Category("Breed", "Wool")]
        [Description("Pregnancy Lactation Adjustment [CW9]")]
        public double PregLactationAdjustment_CW9 { get; set; } = 1.0;

        // CW10 and CW11 - not included - Fibre diameter

        /// <summary>
        /// Age Factor Exponent (SCA CW12)
        /// </summary>
        [Category("Breed", "Wool")]
        [Description("Age Factor Exponent [CW12]")]
        public double AgeFactorExponent_CW12 { get; set; } = 0.025;

        /// <summary>
        /// Create copy of this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            RuminantParametersGrow24CW clonedParameters = new()
            {
                StandardFleeceWeight = StandardFleeceWeight,
                EnergyContentCleanWool_CW1 = EnergyContentCleanWool_CW1,
                BasalCleanWoolGrowth_CW2 = BasalCleanWoolGrowth_CW2,
                CleanToGreasyCRatio_CW3 = CleanToGreasyCRatio_CW3,
                LagFactorForWool_CW4 = LagFactorForWool_CW4,
                WoolGrowthProportionAtBirth_CW5 = WoolGrowthProportionAtBirth_CW5,
                DPLSLimitationForWoolGrowth_CW7 = DPLSLimitationForWoolGrowth_CW7,
                MEILimitationOnWoolGrowth_CW8 = MEILimitationOnWoolGrowth_CW8,
                PregLactationAdjustment_CW9 = PregLactationAdjustment_CW9,
                AgeFactorExponent_CW12 = AgeFactorExponent_CW12
            };
            return clonedParameters;
        }
    }
}
