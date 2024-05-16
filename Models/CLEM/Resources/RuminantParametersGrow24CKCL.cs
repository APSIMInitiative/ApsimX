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
        [System.ComponentModel.DefaultValue(0.4)]
        public double ELactationEfficiencyIntercept_CK5 { get; set; }

        /// <summary>
        /// Energy lactation efficiency coefficient (SCA CK6)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy lactation efficiency coefficient [CK6]")]
        [Required, GreaterThanValue(0), Proportion]
        [System.ComponentModel.DefaultValue(0.02)]
        public double ELactationEfficiencyCoefficient_CK6 { get; set; }

        // CK7 - Not used

        // CK8 - CK16 hard coded

        #endregion

        #region Lactation CL#

        /// <summary>
        /// Peak yield lactation scalar (SCA CL0) 
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Peak lactation yield scalar (CL0)")]
        [Required, MinLength(1)]
        public double[] PeakYieldScalar_CL0 { get; set; } = new double[] { 0.375, 0.375 };

        /// <summary>
        /// Milk offset day (SCA CL1)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk offset day [CL1]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(4)]
        public double MilkOffsetDay_CL1 { get; set; }

        /// <summary>
        /// Milk peak day (SCA CL2)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk peak day [CL2]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(30)]
        public double MilkPeakDay_CL2 { get; set; }

        /// <summary>
        /// Milk curve shape suckling (SCA CL3)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk curve shape suckling [CL3]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.6)]
        public double MilkCurveSuckling_CL3 { get; set; }

        /// <summary>
        /// Milk curve shape non suckling (SCA CL4)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk curve shape non suckling [CL4]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.6)]
        public double MilkCurveNonSuckling_CL4 { get; set; }

        /// <summary>
        /// Metabolisability of milk (SCA CL5)
        /// </summary>
        [Category("Core", "Lactation")]
        [Description("Metabolisability of milk [CL5]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.94)]
        public double MetabolisabilityOfMilk_CL5 { get; set; }

        /// <summary>
        /// Energy content of milk (MJ kg-1, SCA CL6)
        /// </summary>
        [Category("Farm", "Lactation")]
        [Description("Energy content of milk [CL6]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(3.1)]
        public double EnergyContentMilk_CL6 { get; set; }

        /// <summary>
        /// Lactation energy deficit (CL7 in SCA)
        /// </summary>
        [Category("Core", "Lactation")]
        [Description("Lactation energy deficit [CL7]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1.17)]
        public double LactationEnergyDeficit_CL7 { get; set; }

        // CL8 - CL11 Not Used

        /// <summary>
        /// Milk consumption limit 1 (CL12 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("MilkConsumptionLimit1 [CL12]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.42)]
        public double MilkConsumptionLimit1_CL12 { get; set; }

        /// <summary>
        /// Milk consumption limit 2 (CL13 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk consumption limit 2 [CL13]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.58)]
        public double MilkConsumptionLimit2_CL13 { get; set; }

        /// <summary>
        /// Milk consumption limit 3 (CL14 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk consumption limit 3 [CL14]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.036)]
        public double MilkConsumptionLimit3_CL14 { get; set; }

        /// <summary>
        /// Protein content of milk (kg kg-1, SCA CL15)
        /// </summary>
        [Category("Farm", "Lactation")]
        [Description("Protein content of milk [CL15]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.032)]
        public double ProteinContentMilk_CL15 { get; set; }

        /// <summary>
        /// Adjustment of potential lactation yield reduction (CL16 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Adjustment of potential lactation yield reduction [CL16]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.7)]
        public double AdjustmentOfPotentialYieldReduction_CL16 { get; set; }

        /// <summary>
        /// Potential lactation yield reduction (CL17 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield reduction [CL17]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.01)]
        public double PotentialYieldReduction_CL17 { get; set; }

        /// <summary>
        /// Potential lactation yield reduction 2 (CL18 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield reduction 2 [CL18]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.1)]
        public double PotentialYieldReduction2_CL18 { get; set; }

        /// <summary>
        /// Potential lactation yield (CL19 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield [CL19]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1.6)]
        public double PotentialLactationYieldParameter_CL19 { get; set; }

        /// <summary>
        /// Potential lactation yield MEI effect (CL20 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield MEI effect [CL20]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(4.0)]
        public double PotentialYieldMEIEffect_CL20 { get; set; }

        /// <summary>
        /// Potential yield lactation effect 1 (CL21 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential yield lactation effect 1 [CL21]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.004)]
        public double PotentialYieldLactationEffect_CL21 { get; set; }

        /// <summary>
        /// Potential yield lactation effect 2 (CL22 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential yield lactation effect 1 [CL22]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.006)]
        public double PotentialYieldLactationEffect2_CL22 { get; set; }

        /// <summary>
        /// Potential lactation yield condition effect 1 (CL23 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield condition effect [CL23]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(3.0)]
        public double PotentialYieldConditionEffect_CL23 { get; set; }

        /// <summary>
        /// Potential lactation yield condition effect 2 (CL24 in SCA)
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Potential lactation yield condition effect 2 [CL24]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.6)]
        public double PotentialYieldConditionEffect2_CL24 { get; set; }

        #endregion


        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrow24CKCL()
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
                ProteinContentMilk_CL15 = ProteinContentMilk_CL15,
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
