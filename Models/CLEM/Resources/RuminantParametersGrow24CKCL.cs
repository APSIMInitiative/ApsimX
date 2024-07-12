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
    [Description("RuminantActivityGrow24 (CK - efficiency, CL - lactation)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow24CKCL.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrow24CKCL : CLEMModel, ISubParameters, ICloneable
    {
        #region Efficiency of... CK#

        // CK1-CK3 hard coded for efficiency of milk energy used for maintenance.

        // CK4 not used

        /// <summary>
        /// Energy lactation efficiency intercept (SCA CK5)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy lactation efficiency intercept [CK5]")]
        [Required, GreaterThanValue(0), Proportion]
        public double ELactationEfficiencyIntercept_CK5 { get; set; } = 0.4;

        /// <summary>
        /// Energy lactation efficiency coefficient (SCA CK6)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy lactation efficiency coefficient [CK6]")]
        [Required, GreaterThanValue(0), Proportion]
        public double ELactationEfficiencyCoefficient_CK6 { get; set; } = 0.02;

        // CK7 - Not used

        // CK8 - CK16 hard coded

        #endregion

        #region Lactation CL#
        /// <summary>
        /// Expected Peak yield lactation for milking 
        /// </summary>
        [Category("Farm", "Lactation")]
        [Description("Expected peak lactation (kg) when milking")]
        [Required, GreaterThanEqualValue(0)]
        public double ExpectedPeakYield { get; set; } = 20.0;

        /// <summary>
        /// Peak yield lactation scalar (SCA CL0) 
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Peak lactation yield scalar [CL0]")]
        [Required, MinLength(1)]
        public double[] PeakYieldScalar_CL0 { get; set; } = new double[] { 0.375, 0.375 };

        /// <summary>
        /// Milk offset day (SCA CL1)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk offset day [CL1]")]
        [Required, GreaterThanValue(0)]
        public double MilkOffsetDay_CL1 { get; set; } = 4;

        /// <summary>
        /// Milk peak day (SCA CL2)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk peak day [CL2]")]
        [Required, GreaterThanValue(0)]
        public double MilkPeakDay_CL2 { get; set; } = 30;

        /// <summary>
        /// Milk curve shape suckling (SCA CL3)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk curve shape suckling [CL3]")]
        [Required, GreaterThanValue(0)]
        public double MilkCurveSuckling_CL3 { get; set; } = 0.6;

        /// <summary>
        /// Milk curve shape non suckling (SCA CL4)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk curve shape non suckling [CL4]")]
        [Required, GreaterThanValue(0)]
        public double MilkCurveNonSuckling_CL4 { get; set; } = 0.6;

        /// <summary>
        /// Metabolisability of milk (SCA CL5)
        /// </summary>
        [Category("Core", "Lactation")]
        [Description("Metabolisability of milk [CL5]")]
        [Required, GreaterThanValue(0)]
        public double MetabolisabilityOfMilk_CL5 { get; set; } = 0.94;

        /// <summary>
        /// Energy content of milk (MJ kg-1, SCA CL6)
        /// </summary>
        [Category("Farm", "Lactation")]
        [Description("Energy content of milk [CL6]")]
        [Required, GreaterThanValue(0)]
        public double EnergyContentMilk_CL6 { get; set; } = 0.031;

        /// <summary>
        /// Lactation energy deficit (CL7 in SCA)
        /// </summary>
        [Category("Core", "Lactation")]
        [Description("Lactation energy deficit [CL7]")]
        [Required, GreaterThanValue(0)]
        public double LactationEnergyDeficit_CL7 { get; set; } = 1.17;

        // CL8 - CL11 Not Used

        /// <summary>
        /// Milk consumption limit 1 (CL12 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("MilkConsumptionLimit1 [CL12]")]
        [Required, GreaterThanValue(0)]
        public double MilkConsumptionLimit1_CL12 { get; set; } = 0.42;

        /// <summary>
        /// Milk consumption limit 2 (CL13 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk consumption limit 2 [CL13]")]
        [Required, GreaterThanValue(0)]
        public double MilkConsumptionLimit2_CL13 { get; set; } = 0.58;

        /// <summary>
        /// Milk consumption limit 3 (CL14 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk consumption limit 3 [CL14]")]
        [Required, GreaterThanValue(0)]
        public double MilkConsumptionLimit3_CL14 { get; set; } = 0.036;

        /// <summary>
        /// Protein content of milk as percent (%, SCA CL15)
        /// </summary>
        [Category("Farm", "Lactation")]
        [Description("Protein percent of milk [CL15]")]
        [Required, GreaterThanValue(0)]
        [Units("%")]
        public double ProteinPercentMilk_CL15 { get; set; } = 3.2;

        /// <summary>
        /// Adjustment of potential lactation yield reduction (CL16 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Adjustment of potential lactation yield reduction [CL16]")]
        [Required, GreaterThanValue(0)]
        public double AdjustmentOfPotentialYieldReduction_CL16 { get; set; } = 0.7;

        /// <summary>
        /// Potential lactation yield reduction (CL17 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield reduction [CL17]")]
        [Required, GreaterThanValue(0)]
        public double PotentialYieldReduction_CL17 { get; set; } = 0.01;

        /// <summary>
        /// Potential lactation yield reduction 2 (CL18 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield reduction 2 [CL18]")]
        [Required, GreaterThanValue(0)]
        public double PotentialYieldReduction2_CL18 { get; set; } = 0.1;

        /// <summary>
        /// Potential lactation yield (CL19 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield [CL19]")]
        [Required, GreaterThanValue(0)]
        public double PotentialLactationYieldParameter_CL19 { get; set; } = 1.6;

        /// <summary>
        /// Potential lactation yield MEI effect (CL20 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield MEI effect [CL20]")]
        [Required, GreaterThanValue(0)]
        public double PotentialYieldMEIEffect_CL20 { get; set; } = 4.0;

        /// <summary>
        /// Potential yield lactation effect 1 (CL21 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential yield lactation effect 1 [CL21]")]
        [Required, GreaterThanValue(0)]
        public double PotentialYieldLactationEffect_CL21 { get; set; } = 0.004;

        /// <summary>
        /// Potential yield lactation effect 2 (CL22 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential yield lactation effect 1 [CL22]")]
        [Required, GreaterThanValue(0)]
        public double PotentialYieldLactationEffect2_CL22 { get; set; } = 0.006;

        /// <summary>
        /// Potential lactation yield condition effect 1 (CL23 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield condition effect [CL23]")]
        [Required, GreaterThanValue(0)]
        public double PotentialYieldConditionEffect_CL23 { get; set; } = 3.0;

        /// <summary>
        /// Potential lactation yield condition effect 2 (CL24 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield condition effect 2 [CL24]")]
        [Required, GreaterThanValue(0)]
        public double PotentialYieldConditionEffect2_CL24 { get; set; } = 0.6;

        #endregion

        /// <summary>
        /// Create copy of this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            RuminantParametersGrow24CKCL clonedParameters = new()
            {
                ELactationEfficiencyIntercept_CK5 = ELactationEfficiencyIntercept_CK5,
                ELactationEfficiencyCoefficient_CK6 = ELactationEfficiencyCoefficient_CK6,
                PeakYieldScalar_CL0 = PeakYieldScalar_CL0.Clone() as double[],
                MilkOffsetDay_CL1 = MilkOffsetDay_CL1,
                MilkPeakDay_CL2 = MilkPeakDay_CL2,
                MilkCurveSuckling_CL3 = MilkCurveSuckling_CL3,
                MilkCurveNonSuckling_CL4 = MilkCurveNonSuckling_CL4,
                MetabolisabilityOfMilk_CL5 = MetabolisabilityOfMilk_CL5,
                EnergyContentMilk_CL6 = EnergyContentMilk_CL6,
                LactationEnergyDeficit_CL7 = LactationEnergyDeficit_CL7,
                MilkConsumptionLimit1_CL12 = MilkConsumptionLimit1_CL12,
                MilkConsumptionLimit2_CL13 = MilkConsumptionLimit2_CL13,
                MilkConsumptionLimit3_CL14 = MilkConsumptionLimit3_CL14,
                ProteinPercentMilk_CL15 = ProteinPercentMilk_CL15,
                AdjustmentOfPotentialYieldReduction_CL16 = AdjustmentOfPotentialYieldReduction_CL16,
                PotentialYieldReduction_CL17 = PotentialYieldReduction_CL17,
                PotentialYieldReduction2_CL18 = PotentialYieldReduction2_CL18,
                PotentialLactationYieldParameter_CL19 = PotentialLactationYieldParameter_CL19,
                PotentialYieldMEIEffect_CL20 = PotentialYieldMEIEffect_CL20,
                PotentialYieldLactationEffect_CL21 = PotentialYieldLactationEffect_CL21,
                PotentialYieldLactationEffect2_CL22 = PotentialYieldLactationEffect2_CL22,
                PotentialYieldConditionEffect_CL23 = PotentialYieldConditionEffect_CL23,
                PotentialYieldConditionEffect2_CL24 = PotentialYieldConditionEffect2_CL24
            };
            return clonedParameters;
        }
    }
}
