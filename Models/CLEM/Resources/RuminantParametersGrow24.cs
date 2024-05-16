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
    /// This stores the parameters relating to RuminantActivityGrowSCA for a ruminant Type
    /// All default values are provided for cattle and Bos indicus breeds where values apply.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("This model provides all parameters specific to RuminantActivityGrow24")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow24.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrow24CACRD), typeof(RuminantParametersGrow24CD), typeof(RuminantParametersGrow24CG), typeof(RuminantParametersGrow24CI), typeof(RuminantParametersGrow24CKCL), typeof(RuminantParametersGrow24CM), typeof(RuminantParametersGrow24CP) },
        associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Child, ModelAssociationStyle.Child, ModelAssociationStyle.Child, ModelAssociationStyle.Child, ModelAssociationStyle.Child, ModelAssociationStyle.Child, ModelAssociationStyle.Child },
        SingleInstance = true)]
    public class RuminantParametersGrow24 : CLEMModel, ISubParameters, ICloneable
    {
        ///// <summary>
        ///// Conversion from empty body weigh to live weight
        ///// </summary>
        //[Description("Conversion from empty body weigh to live weight")]
        //[Category("Farm", "Growth")]
        //[Required, GreaterThanValue(1.0)]
        //[System.ComponentModel.DefaultValue(1.09)]
        //public double EBW2LW_CG18 { get; set; }

        //#region Rumen Degradability CRD#

        ///// <summary>
        ///// Rumen degradability intercept (SCA CRD1) [Core] [def=] - Growth
        ///// </summary>
        //[Description("Rumen degradability intercept [CRD1]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.3)]
        //public double RumenDegradabilityIntercept_CRD1 { get; set; }

        ///// <summary>
        ///// Rumen degradability slope (SCA CRD2) (SCA CRD1) [Core] [def=] - Growth
        ///// </summary>
        //[Description("Rumen degradability slope [CRD2]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.25)]
        //public double RumenDegradabilitySlope_CRD2 { get; set; }

        ///// <summary>
        ///// Rumen degradability slope for concentrates/supplements (SCA CRD3) [Core] [def=] - Growth
        ///// </summary>
        //[Description("Rumen degradability slope for concentrates [CRD3]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.1)]
        //public double RumenDegradabilityConcentrateSlope_CRD3 { get; set; }

        ///// <summary>
        ///// Rumen degradable protein intercept (SCA CRD4) [Core] [def=] - Growth
        ///// </summary>
        //[Description("Rumen degradable protein intercept [CRD4]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.007)]
        //public double RumenDegradableProteinIntercept_CRD4 { get; set; }

        ///// <summary>
        ///// Rumen degradable protein slope (SCA CRD5) [Core] [def=] - Growth
        ///// </summary>
        //[Description("Rumen degradable protein slope [CRD5]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.005)]
        //public double RumenDegradableProteinSlope_CRD5 { get; set; }

        ///// <summary>
        ///// Rumen degradable protein exponent (SCA CRD6) [Core] [def=] - Growth
        ///// </summary>
        //[Description("Rumen degradable protein exponent [CRD6]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.35)]
        //public double RumenDegradableProteinExponent_CRD6 { get; set; }

        //// rumenDegradableProteinTimeOfYear [CRD7] 0.1 - not used

        ///// <summary>
        ///// Proportion of protein requirement shortfall overcome by recycling to rumen scalar (for tropical breeds)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("N recycling to rumen scalar")]
        //[Required, GreaterThanEqualValue(0)]
        //[System.ComponentModel.DefaultValue(0.0)] // B.indicus 0.5, B.indicus x breeds 0.25
        //public double ProteinShortfallAlleviationScalar { get; set; }

        //#endregion

        //#region CA#

        //// CA1- CA4, CA9 hard coded in DUDP calculations FoodResourceStore.DUDP

        ///// <summary>
        ///// Milk protein digestability (SCA CA5) [Core] - Lactation
        ///// </summary>
        //[Description("Milk protein digestability [CA5]")]
        //[System.ComponentModel.DefaultValue(0.92)]
        //[Category("Breed", "Growth")]
        //[Required, Proportion]
        //public double MilkProteinDigestability_CA5 { get; set; }

        ///// <summary>
        ///// Digestability of microbial protein (SCA CA7) [Core] - gorwth 
        ///// </summary>
        //[Description("Digestability of microbial protein [CA7]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.6)]
        //[Required, Proportion]
        //public double MicrobialProteinDigestibility_CA7 { get; set; }

        ///// <summary>
        ///// Faecal protein from MCP (SCA CA8) [Core] - growth
        ///// </summary>
        //[Description("Faecal protein from MCP [CA8]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.25)]
        //[Required, Proportion]
        //public double FaecalProteinFromMCP_CA8 { get; set; }

        //// UDP digestibility in concentrates

        //#endregion

        //#region Growth CG#

        ///// <summary>
        ///// Efficiency of DPLS use for wool (CG1 in SCA) [Breed] - [0.6] - Wool
        ///// </summary>
        ///// <value>Default is for cattle</value>
        //[Description("Efficiency of DPLS use for wool [CG1]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.6)]
        //[Required, Proportion]
        //public double EfficiencyOfDPLSUseForWool_CG1 { get; set; }

        ///// <summary>
        ///// Efficiency of DPLS use from Feed (CG2 in SCA) [Breed] - [0.7] - Growth
        ///// </summary>
        ///// <value>Default is for cattle</value>
        //[Description("Efficiency of DPLS use from feed [CG2]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.7)]
        //[Required, Proportion]
        //public double EfficiencyOfDPLSUseFromFeed_CG2 { get; set; }

        ///// <summary>
        ///// Efficiency of DPLS use from milk (CG3 in SCA) [Breed] - [0.8] - Growth
        ///// </summary>
        ///// <value>Default is for cattle</value>
        //[Description("Efficiency of DPLS use from milk [CG3]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.8)]
        //[Required, Proportion]
        //public double EfficiencyOfDPLSUseFromMilk_CG3 { get; set; }

        ///// <summary>
        ///// Gain curvature (CG4 in SCA) [breed] - Growth
        ///// </summary>
        ///// <value>Default is for cattle</value>
        //[Description("Gain curvature [CG4]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(6.0)]
        //[Required]
        //public double GainCurvature_CG4 { get; set; }

        ///// <summary>
        ///// Gain midpoint (CG5 in SCA) [breed] - Growth
        ///// </summary>
        ///// <value>Default is for cattle</value>
        //[Description("Gain midpoint [CG5]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.4)]
        //[Required]
        //public double GainMidpoint_CG5 { get; set; }

        ///// <summary>
        ///// Condition no effect (CG6 in SCA)  [breed] - Growth
        ///// </summary>
        ///// <value>Default is for cattle</value>
        //[Description("Condition no effect [CG6]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.9)]
        //[Required]
        //public double ConditionNoEffect_CG6 { get; set; }

        ///// <summary>
        ///// Condition maximum effect (CG7 in SCA) [breed] - Growth
        ///// </summary>
        ///// <value>Default is for cattle</value>
        //[Description("Condition max effect [CG7]")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.97)]
        //[Required]
        //public double ConditionMaxEffect_CG7 { get; set; }

        ///// <summary>
        ///// Intercept parameter for calculation of energy needed per kg empty body gain #1 (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG8)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Energy per kg growth #1 [CG8]")]
        //[System.ComponentModel.DefaultValue(27.0)] // B.indicus 23.2
        //[Required, GreaterThanValue(0)] // [breed] - Growth
        //public double GrowthEnergyIntercept1_CG8 { get; set; }
        
        ///// <summary>
        ///// Intercept Parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG9)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Energy per kg growth #2 [CG9]")]
        //[System.ComponentModel.DefaultValue(20.3)] // B.indicus 16.5
        //[Required, GreaterThanValue(0)] // [breed] - Growth
        //public double GrowthEnergyIntercept2_CG9 { get; set; }

        ///// <summary>
        ///// Slope parameter for calculation of energy needed per kg empty body gain #1 (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG10)
        ///// </summary>
        ///// <values>Default is for cattle</values>
        //[Category("Breed", "Growth")]
        //[Description("Growth energy slope #1 [CG10]")]
        //[System.ComponentModel.DefaultValue(2.0)]
        //[Required, GreaterThanValue(0)] // [breed] - Growth
        //public double GrowthEnergySlope1_CG10 { get; set; }

        ///// <summary>
        ///// Slope parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG11)
        ///// </summary>
        ///// <values>Default is for cattle (20.3), Bos indicus breed value used</values>
        //[Category("Breed", "Growth")]
        //[Description("Energy per kg growth #2 [CG11]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(13.8)]// [breed] - Growth
        //public double GrowthEnergySlope2_CG11 { get; set; }

        ///// <summary>
        ///// First intercept of equation to determine energy protein mass (kg kg-1, SCA CG12)
        ///// </summary>
        //[Description("Protein gain intercept #1 [CG12]")]
        //[Category("Breed", "Growth")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.072)] // B.indicus 0.092 // [breed] - Growth
        //public double ProteinGainIntercept1_CG12 { get; set; }

        ///// <summary>
        ///// Second intercept of equation to determine energy protein mass (kg kg-1, SCA CG13)
        ///// </summary>
        //[Description("Protein gain intercept #2 [CG13]")]
        //[Category("Breed", "Growth")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.140)] // B.indicus 0.120 [breed] - Growth
        //public double ProteinGainIntercept2_CG13 { get; set; }

        ///// <summary>
        ///// First slope of equation to determine energy protein mass (kg kg-1, SCA CG14)
        ///// </summary>
        ///// <values>Default is for cattle</values>
        //[Description("Protein gain slope #1 [CG14]")]
        //[Category("Breed", "Growth")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.008)]// [breed] - Growth
        //public double ProteinGainSlope1_CG14 { get; set; }

        ///// <summary>
        ///// Second slope of equation to determine energy protein mass (kg kg-1, SCA CG15)
        ///// </summary>
        ///// <values>Default is for cattle</values>
        //[Description("Protein gain slope #2 [CG15]")]
        //[Category("Breed", "Growth")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.115)]// [breed] - Growth
        //public double ProteinGainSlope2_CG15 { get; set; }

        ///// <summary>
        ///// Breed growth efficiency scalar
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Breed growth efficiency scalar")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(1)]
        //public double BreedGrowthEfficiencyScalar { get; set; }

        ///// <summary>
        ///// Breed lactation efficiency scalar
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Breed lactation efficiency scalar")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(1)]
        //public double BreedLactationEfficiencyScalar { get; set; }

        ///// <summary>
        ///// Breed maintenance efficiency scalar
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Breed maintenance efficiency scalar")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(1)]
        //public double BreedMainenanceEfficiencyScalar { get; set; }

        //#endregion

        //#region Methane CH#

        ///// <summary>
        ///// 
        ///// </summary>
        //[Description("")]
        //public double CH1 { get; set; }

        ///// <summary>
        ///// 
        ///// </summary>
        //[Description("")]
        //public double CH2 { get; set; }

        ///// <summary>
        ///// 
        ///// </summary>
        //[Description("")]
        //public double CH3 { get; set; }

        ///// <summary>
        ///// 
        ///// </summary>
        //[Description("")]
        //public double CH4 { get; set; }

        ///// <summary>
        ///// 
        ///// </summary>
        //[Description("")]
        //public double CH5 { get; set; }

        //#endregion

        //#region Intake CI#

        ///// <summary>
        ///// Relative size scalar (SCA CI1) [Breed] - Growth
        ///// </summary>
        //[Description("Relative size scalar [CI1]")]
        //[Category("Breed", "Growth")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.025)]
        //public double RelativeSizeScalar_CI1 { get; set; }

        ///// <summary>
        ///// Relative size quadratic (SCA CI2) [Breed] - Growth
        ///// </summary>
        //[Description("Relative size quadratic [CI2]")]
        //[Category("Breed", "Growth")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(1.7)]
        //public double RelativeSizeQuadratic_CI2 { get; set; }

        ///// <summary>
        ///// Rumen Development Curvature (SCA CI3) [Breed] - Growth
        ///// </summary>
        //[Description("Rumen Development Curvature [CI3]")]
        //[Category("Breed", "Growth")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.22)]
        //public double RumenDevelopmentCurvature_CI3 { get; set; }

        ///// <summary>
        ///// Rumen Development Age (SCA CI4)
        ///// </summary>
        //[Description("Rumen Development Age [CI4]")]
        //[Required, GreaterThanValue(0)]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(60)]
        //public double RumenDevelopmentAge_CI4 { get; set; }

        /////// <summary>
        /////// High temperature effect (SCA CI5)
        /////// </summary>
        ////[Category("Breed", "Growth")]
        ////[Description("High temperature effect [CI5]")]
        ////[Required, GreaterThanValue(0)]
        ////[System.ComponentModel.DefaultValue(0.02)]
        ////public double HighTemperatureEffect_CI5 { get; set; }

        /////// <summary>
        /////// Maximum temperature threshold (SCA CI6)
        /////// </summary>
        ////[Category("Breed", "Growth")]
        ////[Description("Maximum temperature threshold [CI6]")]
        ////[Required, GreaterThanValue(0)]
        ////[System.ComponentModel.DefaultValue(25.0)]
        ////public double MaxTemperatureThreshold_CI6 { get; set; }

        /////// <summary>
        /////// Minimum temperature threshold (SCA CI7)
        /////// </summary>
        ////[Category("Breed", "Growth")]
        ////[Description("Minimum temperature threshold [CI7]")]
        ////[Required, GreaterThanValue(0)]
        ////[System.ComponentModel.DefaultValue(22.0)]
        ////public double MinTemperatureThreshold_CI7 { get; set; }

        ///// <summary>
        ///// Peak lactation intake day (SCA CI8)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Peak lactation intake day [CI8]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(62)]
        //public double PeakLactationIntakeDay_CI8 { get; set; }

        ///// <summary>
        ///// Lactation response curvature (SCA CI9)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Lactation response curvature [CI9]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(1.7)]
        //public double LactationResponseCurvature_CI9 { get; set; }

        ///// <summary>
        ///// Effect of levels of milk prodiction on intake -  Dairy cows  (SCA CI10)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Effect of levels of milk prodiction on intake [CI10]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.6)]
        //public double EffectLevelsMilkProdOnIntake_CI10 { get; set; }

        ///// <summary>
        ///// Basal milk relative to SRW - Dairy cows  (SCA CI11)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Basal milk relative to SRW [CI11]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.05)]
        //public double BasalMilkRelSRW_CI11 { get; set; }

        ///// <summary>
        ///// Lactation Condition Loss Adjustment (SCA CI12)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Lactation Condition Loss Adjustment [CI12]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.15)]
        //public double LactationConditionLossAdjustment_CI12 { get; set; }

        ///// <summary>
        ///// Lactation Condition Loss Threshold (SCA CI13)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Lactation Condition Loss Threshold [CI13]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.005)]
        //public double LactationConditionLossThreshold_CI13 { get; set; }

        ///// <summary>
        ///// Lactation condition loss threshold decay (SCA CI14)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Lactation condition loss threshold decay [CI14]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.002)]
        //public double LactationConditionLossThresholdDecay_CI14 { get; set; }

        ///// <summary>
        ///// Condition at parturition adjustment (SCA CI15)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Condition at parturition adjustment [CI15]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.5)]
        //public double ConditionAtParturitionAdjustment_CI15 { get; set; }

        //// CI16 EMPTY

        /////// <summary>
        /////// Low temperature effect (SCA CI17)
        /////// </summary>
        ////[Description("Low temperature effect [CI17]")]
        ////[Required, GreaterThanValue(0)]
        ////[System.ComponentModel.DefaultValue(0.01)]
        ////public double LowTemperatureEffect_CI17 { get; set; }

        /////// <summary>
        /////// Rainfall scalar (SCA CI18)
        /////// </summary>
        ////[Description("Rainfall scalar [CI18]")]
        ////[Required, GreaterThanValue(0)]
        ////[System.ComponentModel.DefaultValue(20.0)]
        ////public double RainfallScalar_CI18 { get; set; }

        ///// <summary>
        ///// Peak lactation intake level (SCA CI19)
        ///// </summary>
        //[Category("Farm", "Lactation")]
        //[Description("Peak lactation intake level [CI19]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(new[] { 0.416, 0.416 })]
        //public double[] PeakLactationIntakeLevel_CI19 { get; set; }

        ///// <summary>
        ///// Relative condition effect (SCA CI20)
        ///// </summary>
        //[Category("Farm", "Lactation")]
        //[Description("Relative condition effect [CI20]")]
        //[Required, GreaterThanValue(1)]
        //[System.ComponentModel.DefaultValue(1.5)]
        //public double RelativeConditionEffect_CI20 { get; set; }

        //#endregion

        //#region Efficiency of... CK#

        //// CK1-CK3 hard coded for efficiency of milk energy used for maintenance.

        //// CK4 not used

        ///// <summary>
        ///// Energy lactation efficiency intercept (SCA CK5)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Energy lactation efficiency intercept [CK5]")]
        //[Required, GreaterThanValue(0), Proportion]
        //[System.ComponentModel.DefaultValue(0.4)]
        //public double ELactationEfficiencyIntercept_CK5 { get; set; }

        ///// <summary>
        ///// Energy lactation efficiency coefficient (SCA CK6)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Energy lactation efficiency coefficient [CK6]")]
        //[Required, GreaterThanValue(0), Proportion]
        //[System.ComponentModel.DefaultValue(0.02)]
        //public double ELactationEfficiencyCoefficient_CK6 { get; set; }

        //// CK7 - Not used

        //// CK8 - CK16 hard coded

        //#endregion

        //#region Lactation CL#

        ///// <summary>
        ///// Peak yield lactation scalar (SCA CL0) 
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Peak lactation yield scalar (CL0)")]
        //[Required, MinLength(1)]
        //public double[] PeakYieldScalar_CL0 { get; set; } = new double[] { 0.375, 0.375 };

        ///// <summary>
        ///// Milk offset day (SCA CL1)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Milk offset day [CL1]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(4)]
        //public double MilkOffsetDay_CL1 { get; set; }

        ///// <summary>
        ///// Milk peak day (SCA CL2)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Milk peak day [CL2]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(30)]
        //public double MilkPeakDay_CL2 { get; set; }

        ///// <summary>
        ///// Milk curve shape suckling (SCA CL3)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Milk curve shape suckling [CL3]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.6)]
        //public double MilkCurveSuckling_CL3 { get; set; }

        ///// <summary>
        ///// Milk curve shape non suckling (SCA CL4)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Milk curve shape non suckling [CL4]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.6)]
        //public double MilkCurveNonSuckling_CL4 { get; set; }

        ///// <summary>
        ///// Metabolisability of milk (SCA CL5)
        ///// </summary>
        //[Category("Core", "Lactation")]
        //[Description("Metabolisability of milk [CL5]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.94)]
        //public double MetabolisabilityOfMilk_CL5 { get; set; }

        ///// <summary>
        ///// Energy content of milk (MJ kg-1, SCA CL6)
        ///// </summary>
        //[Category("Farm", "Lactation")]
        //[Description("Energy content of milk [CL6]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(3.1)]
        //public double EnergyContentMilk_CL6 { get; set; }

        ///// <summary>
        ///// Lactation energy deficit (CL7 in SCA)
        ///// </summary>
        //[Category("Core", "Lactation")]
        //[Description("Lactation energy deficit [CL7]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(1.17)]
        //public double LactationEnergyDeficit_CL7 { get; set; }

        //// CL8 - CL11 Not Used

        ///// <summary>
        ///// Milk consumption limit 1 (CL12 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("MilkConsumptionLimit1 [CL12]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.42)]
        //public double MilkConsumptionLimit1_CL12 { get; set; }

        ///// <summary>
        ///// Milk consumption limit 2 (CL13 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Milk consumption limit 2 [CL13]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.58)]
        //public double MilkConsumptionLimit2_CL13 { get; set; }

        ///// <summary>
        ///// Milk consumption limit 3 (CL14 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Milk consumption limit 3 [CL14]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.036)]
        //public double MilkConsumptionLimit3_CL14 { get; set; }

        ///// <summary>
        ///// Protein content of milk (kg kg-1, SCA CL15)
        ///// </summary>
        //[Category("Farm", "Lactation")]
        //[Description("Protein content of milk [CL15]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.032)]
        //public double ProteinContentMilk_CL15 { get; set; }

        ///// <summary>
        ///// Adjustment of potential lactation yield reduction (CL16 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Adjustment of potential lactation yield reduction [CL16]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.7)]
        //public double AdjustmentOfPotentialYieldReduction_CL16 { get; set; }

        ///// <summary>
        ///// Potential lactation yield reduction (CL17 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Potential lactation yield reduction [CL17]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.01)]
        //public double PotentialYieldReduction_CL17 { get; set; }

        ///// <summary>
        ///// Potential lactation yield reduction 2 (CL18 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Potential lactation yield reduction 2 [CL18]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.1)]
        //public double PotentialYieldReduction2_CL18 { get; set; }

        ///// <summary>
        ///// Potential lactation yield (CL19 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Potential lactation yield [CL19]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(1.6)]
        //public double PotentialLactationYieldParameter_CL19 { get; set; }

        ///// <summary>
        ///// Potential lactation yield MEI effect (CL20 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Potential lactation yield MEI effect [CL20]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(4.0)]
        //public double PotentialYieldMEIEffect_CL20 { get; set; }

        ///// <summary>
        ///// Potential yield lactation effect 1 (CL21 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Potential yield lactation effect 1 [CL21]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.004)]
        //public double PotentialYieldLactationEffect_CL21 { get; set; }

        ///// <summary>
        ///// Potential yield lactation effect 2 (CL22 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Potential yield lactation effect 1 [CL22]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.006)]
        //public double PotentialYieldLactationEffect2_CL22 { get; set; }

        ///// <summary>
        ///// Potential lactation yield condition effect 1 (CL23 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Potential lactation yield condition effect [CL23]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(3.0)]
        //public double PotentialYieldConditionEffect_CL23 { get; set; }

        ///// <summary>
        ///// Potential lactation yield condition effect 2 (CL24 in SCA)
        ///// </summary>
        //[Category("Breed", "Lactation")]
        //[Description("Potential lactation yield condition effect 2 [CL24]")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.6)]
        //public double PotentialYieldConditionEffect2_CL24 { get; set; }

        //#endregion

        //#region Metabolism CM#

        ///// <summary>
        ///// Heat production viscera feed level (CM1 in SCA)
        ///// </summary>
        //[Category("Core", "Growth")]
        //[Description("Heat production viscera feed level [CM1]")]
        //[System.ComponentModel.DefaultValue(0.09)]
        //public double HPVisceraFL_CM1 { get; set; }

        ///// <summary>
        ///// Feed heat production scalar (CM2 in SCA)
        ///// </summary>
        ///// <value>Default is for cattle. Value for Bos indicus breeds with all other cattle 0.36</value>
        //[Category("Breed", "Growth")]
        //[Description("Feed heat production scalar [CM2]")]
        //[System.ComponentModel.DefaultValue(0.36)] // B indicus 0.31
        //public double FHPScalar_CM2 { get; set; }

        ///// <summary>
        ///// Maintenance exponent for age (SCA CM3)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Maintenance exponent for age [CM3]")]
        //[System.ComponentModel.DefaultValue(8e-5)]
        //public double MainExponentForAge_CM3 { get; set; }

        ///// <summary>
        ///// Age effect min (SCA CM4)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Age effect min [CM4]")]
        //[System.ComponentModel.DefaultValue(0.84)]
        //public double AgeEffectMin_CM4 { get; set; }

        ///// <summary>
        ///// Milk scalar (SCA CM5)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Milk scalar [CM5]")]
        //[System.ComponentModel.DefaultValue(0.23)]
        //public double MilkScalar_CM5 { get; set; }

        //// Not in SCA tables. CLEM work around
        //// all converted into grazing energy factor in grazing.
        ///// <summary>
        ///// Grazing energy as proportion of metabolic energy
        ///// </summary>
        //[Description("Grazing energy from metabolic scalar")]
        //[Category("Breed", "Growth")]
        //[System.ComponentModel.DefaultValue(0.2)]
        //public double GrazingEnergyFromMetabolicScalar { get; set; }

        //// chewing scalar CM6

        //// digestability on chewing CM7

        //// Walking Slope CM8

        //// Walking intercept CM9

        //// Solid diet EFP CM10 - hard coded

        //// milk diet EFP CM11 - hard coded

        ///// <summary>
        ///// Breed EUP Factor #1 (SCA CM12)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Breed EUP Factor #1 [CM12]")]
        //[System.ComponentModel.DefaultValue(1.61e-2)] // B.indicus 1.29e-2
        //public double BreedEUPFactor1_CM12 { get; set; }

        ///// <summary>
        ///// Breed EUP Factor #2 (SCA CM13)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Breed EUP Factor #2 [CM13]")]
        //[System.ComponentModel.DefaultValue(4.22e-2)] // B.indicus 3.38e-2
        //public double BreedEUPFactor2_CM13 { get; set; }

        ///// <summary>
        ///// Dermal loss (SCA CM14)
        ///// </summary>
        //[Category("Breed", "Growth")]
        //[Description("Dermal loss [CM14]")]
        //[System.ComponentModel.DefaultValue(1.1e-4)]
        //public double DermalLoss_CM14 { get; set; }

        //// sme CM15 - hard coded

        //// energy cost walking CM16

        //// threshold stocking density CM17

        //#endregion

        //#region Pregnancy C#

        //// CP1 gestation length (see Parameters.General.GestationLength) 

        ///// <summary>
        ///// Fetal normalised weight parameter (SCA CP2)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Fetal normalised weight parameter [CP2]")]
        //[System.ComponentModel.DefaultValue(2.2)]
        //public double FetalNormWeightParameter_CP2 { get; set; }

        ///// <summary>
        ///// Fetal normalised weight parameter #2 (SCA CP3)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Fetal normalised weight parameter 2 [CP3]")]
        //[System.ComponentModel.DefaultValue(1.77)]
        //public double FetalNormWeightParameter2_CP3 { get; set; }

        ///// <summary>
        ///// Effect fetal relative size on birth weight (SCA CP4)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Effect fetal relative size on birth weight [CP4]")]
        //[System.ComponentModel.DefaultValue(0.33)]
        //public double EffectFetalRelativeSizeOnBirthWeight_CP4 { get; set; }

        ///// <summary>
        ///// Conceptus weight ratio (SCA CP5)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Conceptus weight ratio [CP5]")]
        //[System.ComponentModel.DefaultValue(1.8)]
        //public double ConceptusWeightRatio_CP5 { get; set; }

        ///// <summary>
        ///// Conceptus weight parameter (SCA CP6)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Conceptus weight parameter [CP6]")]
        //[System.ComponentModel.DefaultValue(2.42)]
        //public double ConceptusWeightParameter_CP6 { get; set; }

        ///// <summary>
        ///// Conceptus weight parameter #2 (SCA CP7)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Conceptus weight parameter 2 [CP7]")]
        //[System.ComponentModel.DefaultValue(1.16)]
        //public double ConceptusWeightParameter2_CP7 { get; set; }

        ///// <summary>
        ///// Conceptus energy content (SCA CP8)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Conceptus energy content [CP8]")]
        //[System.ComponentModel.DefaultValue(4.11)]
        //public double ConceptusEnergyContent_CP8 { get; set; }

        ///// <summary>
        ///// Conceptus energy parameter (SCA CP9)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Conceptus weight parameter [CP9]")]
        //[System.ComponentModel.DefaultValue(343.5)]
        //public double ConceptusEnergyParameter_CP9 { get; set; }

        ///// <summary>
        ///// Conceptus energy parameter #2 (SCA CP10)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Conceptus weight parameter 2 [CP10]")]
        //[System.ComponentModel.DefaultValue(0.0164)]
        //public double ConceptusEnergyParameter2_CP10 { get; set; }

        ///// <summary>
        ///// Conceptus protein content (SCA CP11)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Conceptus protein content [CP11]")]
        //[System.ComponentModel.DefaultValue(0.134)]
        //public double ConceptusProteinContent_CP11 { get; set; }

        ///// <summary>
        ///// Conceptus protein parameter (SCA CP12)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Conceptus protein parameter [CP12]")]
        //[System.ComponentModel.DefaultValue(6.22)]
        //public double ConceptusProteinParameter_CP12 { get; set; }

        ///// <summary>
        ///// Conceptus protein parameter #2 (SCA CP13)
        ///// </summary>
        //[Category("Breed", "Pregnancy")]
        //[Description("Conceptus protein parameter 2 [CP13]")]
        //[System.ComponentModel.DefaultValue(0.747)]
        //public double ConceptusProteinParameter2_CP13 { get; set; }

        ///// <summary>
        ///// Fetal growth in poor condition for 1,2,3,... young (SCA CP14)
        ///// </summary>
        //[Category("Breed", "Breeding")]
        //[Description("Fetal growth in poor condition [CP14]")]
        //[Required, MinLength(1)]
        //[System.ComponentModel.DefaultValue(new[] { 1.0, 1.15 })]
        //public double[] FetalGrowthPoorCondition_CP14 { get; set; }

        //// CP15 relative size birth weight (see Parameters.General.BirthScalar) 

        //#endregion

        //#region Mortality CD#

        ///// <summary>
        ///// Basal mortality rate CD1
        ///// </summary>
        //[Category("Farm", "Survival")]
        //[Description("Basal mortality rate")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(5.53e-5)]
        //public double BasalMortalityRate_CD1 { get; set; }
        
        ///// <summary>
        ///// Effect body condition on mortality # 1 CD2
        ///// </summary>
        //[Category("Farm", "Survival")]
        //[Description("Effect body condition on mortality #1")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.3)]
        //public double EffectBCOnMortality1_CD2 { get; set; }

        ///// <summary>
        ///// Effect body condition on mortality # 2 CD3
        ///// </summary>
        //[Category("Farm", "Survival")]
        //[Description("Effect body condition on mortality #2")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(0.6)]
        //public double EffectBCOnMortality2_CD3 { get; set; }

        ///// <summary>
        ///// Lower bound for pregnancy toximia 
        ///// </summary>
        //[Category("Farm", "Survival")]
        //[Description("Lower bound for pregnancy toximia")]
        //[Required, GreaterThanEqualValue(0)]
        //[System.ComponentModel.DefaultValue(0.0)]
        //public double LowerBoundsPregnancyToximia_CD4 { get; set; }

        ///// <summary>
        ///// Upper bound for pregnancy toximia 
        ///// </summary>
        //[Category("Farm", "Survival")]
        //[Description("Upper bound for pregnancy toximia")]
        //[Required, GreaterThanEqualValue(0)]
        //[System.ComponentModel.DefaultValue(0.0)]
        //public double UpperBoundsPregnancyToximia_CD5 { get; set; }

        ///// <summary>
        ///// Lower bound for pregnancy dystocia
        ///// </summary>
        //[Category("Farm", "Survival")]
        //[Description("Lower bound for pregnancy dystocia")]
        //[Required, GreaterThanEqualValue(0)]
        //[System.ComponentModel.DefaultValue(0.0)]
        //public double LowerBoundsPregnancyDystocia_CD6 { get; set; }

        ///// <summary>
        ///// Upper bound for pregnancy dystocia 
        ///// </summary>
        //[Category("Farm", "Survival")]
        //[Description("Upper bound for pregnancy dystocia")]
        //[Required, GreaterThanEqualValue(0)]
        //[System.ComponentModel.DefaultValue(0.0)]
        //public double UpperBoundsPregnancyDystocia_CD7 { get; set; }

        //// Add dystocia for cattle at some stage.

        //// CD8 -CD11 Chilling effect - not implemented

        ///// <summary>
        ///// Relative difference in weight of dying animals
        ///// </summary>
        //[Category("Farm", "Survival")]
        //[Description("Relative difference in weight of dying animals")]
        //[System.ComponentModel.DefaultValue(0.1)]
        //[Required, GreaterThanValue(0)]
        //public double RelativeDifferenceWeightDyingIndividuals_CD12 { get; set; }

        ///// <summary>
        ///// Upper limit for mortality in weaners 
        ///// </summary>
        //[Category("Farm", "Survival")]
        //[Description("Upper limit for mortality in weaners")]
        //[Required, GreaterThanValue(0)]
        //[System.ComponentModel.DefaultValue(1e-4)]  //ToDo find suitable default.. This is about 2x base mortality for now. So 2x mort at weaning dropping to base by 1 year old.
        //public double UpperLimitForMortalityInWeaners_CD13 { get; set; }

        //// CD14 age where indivudal condidered wenaer for mort.

        //// CD15 upper age of reduction in mort for weaners. Set in code based on weaner rule (12 months).

        //#endregion


        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrow24()
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
            throw new NotImplementedException();
        }
    }
}
