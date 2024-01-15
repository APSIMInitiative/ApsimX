using Models.Core;
using System;
using System.ComponentModel.DataAnnotations;

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
    public class RuminantParametersGrowSCA : CLEMModel
    {
        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double EBW2LW { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double Prt2NMilk { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double Prt2NTissue { get; set; }

        #region Rumen Degradability CRD#

        /// <summary>
        /// Rumen degradability intercept (SCA CRD1)
        /// </summary>
        [Description("Rumen degradability intercept [CRD1]")]
        [System.ComponentModel.DefaultValue(0.3)]
        public double RumenDegradabilityIntercept_CRD1 { get; set; }

        /// <summary>
        /// Rumen degradability slope (SCA CRD2)
        /// </summary>
        [Description("Rumen degradability slope [CRD2]")]
        [System.ComponentModel.DefaultValue(0.25)]
        public double RumenDegradabilitySlope_CRD2 { get; set; }

        /// <summary>
        /// Rumen degradability slope for concentrates/supplements (SCA CRD3)
        /// </summary>
        [Description("Rumen degradability slope for concentrates [CRD3]")]
        [System.ComponentModel.DefaultValue(0.1)]
        public double RumenDegradabilityConcentrateSlope_CRD3 { get; set; }

        /// <summary>
        /// Rumen degradable protein intercept (SCA CRD4)
        /// </summary>
        [Description("Rumen degradable protein intercept [CRD4]")]
        [System.ComponentModel.DefaultValue(0.007)]
        public double RumenDegradableProteinIntercept_CRD4 { get; set; }

        /// <summary>
        /// Rumen degradable protein slope (SCA CRD5)
        /// </summary>
        [Description("Rumen degradable protein slope [CRD5]")]
        [System.ComponentModel.DefaultValue(0.005)]
        public double RumenDegradableProteinSlope_CRD5 { get; set; }

        /// <summary>
        /// Rumen degradable protein exponent (SCA CRD6)
        /// </summary>
        [Description("Rumen degradable protein exponent [CRD6]")]
        [System.ComponentModel.DefaultValue(0.35)]
        public double RumenDegradableProteinExponent_CRD6 { get; set; }

        // rumenDegradableProteinTimeOfYear [CRD7] 0.1

        #endregion

        #region CA#

        /// <summary>
        /// Milk protein digestability (SCA CA5)
        /// </summary>
        [Description("Milk protein digestability [CA5]")]
        [System.ComponentModel.DefaultValue(0.92)]
        public double MilkProteinDigestability_CA5 { get; set; }

        /// <summary>
        /// Digestability of microbial protein (SCA CA7)
        /// </summary>
        [Description("Digestability of microbial protein [CA7]")]
        [System.ComponentModel.DefaultValue(0.6)]
        public double MicrobialProteinDigestibility_CA7 { get; set; }

        /// <summary>
        /// Faecal protein from MCP (SCA CA8)
        /// </summary>
        [Description("Faecal protein from MCP [CA8]")]
        [System.ComponentModel.DefaultValue(0.25)]
        public double FaecalProteinFromMCP_CA8 { get; set; }

        // UDP digestibility in concentrates

        #endregion

        #region Growth CG#

        /// <summary>
        /// Gain curvature (CG4 in SCA)
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Gain curvature [CG4]")]
        [System.ComponentModel.DefaultValue(6.0)]
        [Required, Proportion]
        public double GainCurvature_CG4 { get; set; }

        /// <summary>
        /// Gain midpoint (CG5 in SCA)
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Gain midpoint [CG5]")]
        [System.ComponentModel.DefaultValue(0.4)]
        [Required, Proportion]
        public double GainMidpoint_CG5 { get; set; }

        /// <summary>
        /// Condition no effect (CG6 in SCA)
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Condition no effect [CG6]")]
        [System.ComponentModel.DefaultValue(0.9)]
        [Required, Proportion]
        public double ConditionNoEffect_CG6 { get; set; }

        /// <summary>
        /// Condition maximum effect (CG7 in SCA)
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Condition max effect [CG7]")]
        [System.ComponentModel.DefaultValue(0.97)]
        [Required, Proportion]
        public double ConditionMaxEffect_CG7 { get; set; }

        /// <summary>
        /// Intercept parameter for calculation of energy needed per kg empty body gain #1 (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG8)
        /// </summary>
        /// <values>Default is for cattle (27.0), Bos indicus breed value used</values>
        [Category("Advanced", "Growth")]
        [Description("Energy per kg growth #1 [CG8]")]
        [System.ComponentModel.DefaultValue(23.2)]
        [Required, GreaterThanValue(0)]
        public double GrowthEnergyIntercept1_CG8 { get; set; }
        /// <summary>
        /// Intercept Parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG9)
        /// </summary>
        /// <values>Default is for cattle (20.3), Bos indicus breed value used</values>
        [Category("Advanced", "Growth")]
        [Description("Energy per kg growth #2 [CG9]")]
        [Required, GreaterThanValue(16.5)]
        public double GrowthEnergyIntercept2_CG9 { get; set; }

        /// <summary>
        /// Slope parameter for calculation of energy needed per kg empty body gain #1 (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG10)
        /// </summary>
        /// <values>Default is for cattle</values>
        [Category("Advanced", "Growth")]
        [Description("Growth energy slope #1 [CG10]")]
        [System.ComponentModel.DefaultValue(2.0)]
        [Required, GreaterThanValue(0)]
        public double GrowthEnergySlope1_CG10 { get; set; }
        /// <summary>
        /// Slope parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG11)
        /// </summary>
        /// <values>Default is for cattle (20.3), Bos indicus breed value used</values>
        [Category("Advanced", "Growth")]
        [Description("Energy per kg growth #2 [CG11]")]
        [Required, GreaterThanValue(13.8)]
        public double GrowthEnergySlope2_CG11 { get; set; }

        /// <summary>
        /// First intercept of equation to determine energy protein mass (kg kg-1, SCA CG12)
        /// </summary>
        /// <values>Default is for cattle (0.072), Bos indicus breed value used</values>
        [Description("Protein gain intercept #1 [CG12]")]
        [Required, GreaterThanValue(0.092)]
        public double ProteinGainIntercept1_CG12 { get; set; }

        /// <summary>
        /// Second intercept of equation to determine energy protein mass (kg kg-1, SCA CG13)
        /// </summary>
        /// <values>Default is for cattle (0.140), Bos indicus breed value used</values>
        [Description("Protein gain intercept #2 [CG13]")]
        [Required, GreaterThanValue(0.120)]
        public double ProteinGainIntercept2_CG13 { get; set; }

        /// <summary>
        /// First slope of equation to determine energy protein mass (kg kg-1, SCA CG14)
        /// </summary>
        /// <values>Default is for cattle</values>
        [Description("Protein gain slope #1 [CG14]")]
        [Required, GreaterThanValue(0.008)]
        public double ProteinGainSlope1_CG14 { get; set; }

        /// <summary>
        /// Second slope of equation to determine energy protein mass (kg kg-1, SCA CG15)
        /// </summary>
        /// <values>Default is for cattle</values>
        [Description("Protein gain slope #2 [CG15]")]
        [Required, GreaterThanValue(0.115)]
        public double ProteinGainSlope2_CG15 { get; set; }

        #endregion

        #region Methane CH#

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CH1 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CH2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CH3 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CH4 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CH5 { get; set; }

        #endregion

        #region Efficiency of... CK#

        // CK3 Efficiency of milk energy used for maintenance 0.85

        /// <summary>
        /// Energy lactation efficiency intercept (SCA CK5)
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy lactation efficiency intercept [CK5]")]
        [Required, GreaterThanValue(0)]
        public double ELactationEfficiencyIntercept_CK5 { get; set; }
        /// <summary>
        /// Energy lactation efficiency coefficient (SCA CK6)
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy lactation efficiency coefficient [CK6]")]
        [Required, GreaterThanValue(0)]
        public double ELactationEfficiencyCoefficient_CK6 { get; set; }

        #endregion

        #region Lactation CL#

        /// <summary>
        /// Peak yield lactation scalar (SCA CL0)
        /// </summary>
        [Description("Peak lactation yield scalar (CL0)")]
        [Required, MinLength(1)]
        [System.ComponentModel.DefaultValue(0.375)]
        public double[] PeakYieldScalar_CL0 { get; set; }

        /// <summary>
        /// Milk offset day (SCA CL1)
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Milk offset day [CL1]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(4)]
        public double MilkOffsetDay_CL1 { get; set; }

        /// <summary>
        /// Milk peak day (SCA CL2)
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Milk peak day [CL2]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(30)]
        public double MilkPeakDay_CL2 { get; set; }

        /// <summary>
        /// Milk curve shape suckling (SCA CL3)
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Milk curve shape suckling [CL3]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.6)]
        public double MilkCurveSuckling_CL3 { get; set; }
        /// <summary>
        /// Milk curve shape non suckling (SCA CL4)
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Milk curve shape non suckling [CL4]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.6)]
        public double MilkCurveNonSuckling_CL4 { get; set; }

        /// <summary>
        /// Metabolisability of milk (SCA CL5)
        /// </summary>
        [Description("Metabolisability of milk [CL5]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.94)]
        public double MetabolisabilityOfMilk_CL5 { get; set; }

        /// <summary>
        /// Energy content of milk (MJ kg-1, SCA CL6)
        /// </summary>
        [Description("Energy content of milk [CL6]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(3.1)]
        public double EnergyContentMilk_CL6 { get; set; }

        /// <summary>
        /// Lactation energy deficit (CL7 in SCA)
        /// </summary>
        [Description("Lactation energy deficit [CL7]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1.17)]
        public double LactationEnergyDeficit_CL7 { get; set; }

        /// <summary>
        /// Milk consumption limit 1 (CL12 in SCA)
        /// </summary>
        [Description("MilkConsumptionLimit1 [CL12]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.42)]
        public double MilkConsumptionLimit1_CL12 { get; set; }

        /// <summary>
        /// Milk consumption limit 2 (CL13 in SCA)
        /// </summary>
        [Description("Milk consumption limit 2 [CL13]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.58)]
        public double MilkConsumptionLimit2_CL13 { get; set; }

        /// <summary>
        /// Milk consumption limit 3 (CL14 in SCA)
        /// </summary>
        [Description("Milk consumption limit 3 [CL14]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.036)]
        public double MilkConsumptionLimit3_CL14 { get; set; }

        /// <summary>
        /// Protein content of milk (kg kg-1, SCA CL15)
        /// </summary>
        [Description("Protein content of milk [CL15]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.032)]
        public double ProteinContentMilk_CL15 { get; set; }

        // potential yield reduction CL16

        /// <summary>
        /// Potential lactation yield reduction (CL17 in SCA)
        /// </summary>
        [Description("Potential lactation yield reduction [CL17]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.01)]
        public double PotentialYieldReduction_CL17 { get; set; }

        /// <summary>
        /// Potential lactation yield reduction 2 (CL18 in SCA)
        /// </summary>
        [Description("Potential lactation yield reduction 2 [CL18]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.1)]
        public double PotentialYieldReduction2_CL18 { get; set; }

        /// <summary>
        /// Potential lactation yield (CL19 in SCA)
        /// </summary>
        [Description("Potential lactation yield [CL19]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1.6)]
        public double PotentialLactationYieldParameter_CL19 { get; set; }

        /// <summary>
        /// Potential lactation yield MEI effect (CL20 in SCA)
        /// </summary>
        [Description("Potential lactation yield MEI effect [CL20]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(4.0)]
        public double PotentialYieldMEIEffect_CL20 { get; set; }


        /// <summary>
        /// Potential yield lactation effect 1 (CL21 in SCA)
        /// </summary>
        [Description("Potential yield lactation effect 1 [CL21]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.004)]
        public double PotentialYieldLactationEffect_CL21 { get; set; }

        /// <summary>
        /// Potential yield lactation effect 2 (CL22 in SCA)
        /// </summary>
        [Description("Potential yield lactation effect 1 [CL22]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.006)]
        public double PotentialYieldLactationEffect2_CL22 { get; set; }

        /// <summary>
        /// Potential lactation yield condition effect 1 (CL23 in SCA)
        /// </summary>
        [Description("Potential lactation yield condition effect [CL23]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(3.0)]
        public double PotentialYieldConditionEffect_CL23 { get; set; }

        /// <summary>
        /// Potential lactation yield condition effect 2 (CL24 in SCA)
        /// </summary>
        [Description("Potential lactation yield condition effect 2 [CL24]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.6)]
        public double PotentialYieldConditionEffect2_CL24 { get; set; }

        #endregion

        #region Metabolism CM#

        /// <summary>
        /// Heat production viscera feed level (CM1 in SCA)
        /// </summary>
        [Description("Heat production viscera feed level [CM1]")]
        [System.ComponentModel.DefaultValue(0.09)]
        public double HPVisceraFL_CM1 { get; set; }

        /// <summary>
        /// Feed heat production scalar (CM2 in SCA)
        /// </summary>
        /// <value>Default is for cattle. Value for Bos indicus breeds with all other cattle 0.36</value>
        [Description("Feed heat production scalar [CM2]")]
        [System.ComponentModel.DefaultValue(0.31)]
        public double FHPScalar_CM2 { get; set; }

        /// <summary>
        /// Maintenance exponent for age (SCA CM3)
        /// </summary>
        [Description("Maintenance exponent for age [CM3]")]
        public double MainExponentForAge_CM3 { get; set; }

        /// <summary>
        /// Age effect min (SCA CM4)
        /// </summary>
        [Description("Age effect min [CM4]")]
        public double AgeEffectMin_CM4 { get; set; }

        /// <summary>
        /// Milk scalar (SCA CM5)
        /// </summary>
        [Description("Milk scalar [CM5]")]
        public double MilkScalar_CM5 { get; set; }

        // chewing scalar CM6

        // digestability on chewing CM7

        // Walking Slope CM8

        // Walking intercept CM9

        // Solid diet EFP CM10

        // milk diet EFP CM11

        /// <summary>
        /// (SCA CM12)
        /// </summary>
        [Description("[CM12]")]
        [System.ComponentModel.DefaultValue(1.29e-2)]
        public double BreedEUPFactor1_CM12 { get; set; }

        /// <summary>
        /// (SCA CM13)
        /// </summary>
        [Description(" [CM13]")]
        [System.ComponentModel.DefaultValue(3.38e-2)]
        public double BreedEUPFactor2_CM13 { get; set; }

        // dermal loss CM14

        // sme CM15

        // energy cost walking CM16

        // threshold stocking density CM17

        #endregion

        #region Pregnancy C#

        // CP1 gestation length

        /// <summary>
        /// Fetal normalised weight parameter (SCA CP2)
        /// </summary>
        [Description("Fetal normalised weight parameter [CP2]")]
        [System.ComponentModel.DefaultValue(2.2)]
        public double FetalNormWeightParameter_CP2 { get; set; }

        /// <summary>
        /// Fetal normalised weight parameter #2 (SCA CP3)
        /// </summary>
        [Description("Fetal normalised weight parameter 2 [CP3]")]
        [System.ComponentModel.DefaultValue(1.77)]
        public double FetalNormWeightParameter2_CP3 { get; set; }

        // CP4 relative size birth weight

        /// <summary>
        /// Conceptus weight ratio (SCA CP5)
        /// </summary>
        [Description("Conceptus weight ratio [CP5]")]
        [System.ComponentModel.DefaultValue(1.8)]
        public double ConceptusWeightRatio_CP5 { get; set; }

        /// <summary>
        /// Conceptus weight parameter (SCA CP6)
        /// </summary>
        [Description("Conceptus weight parameter [CP6]")]
        [System.ComponentModel.DefaultValue(2.42)]
        public double ConceptusWeightParameter_CP6 { get; set; }

        /// <summary>
        /// Conceptus weight parameter #2 (SCA CP7)
        /// </summary>
        [Description("Conceptus weight parameter 2 [CP7]")]
        [System.ComponentModel.DefaultValue(1.16)]
        public double ConceptusWeightParameter2_CP7 { get; set; }

        /// <summary>
        /// Conceptus energy content (SCA CP8)
        /// </summary>
        [Description("Conceptus energy content [CP8]")]
        [System.ComponentModel.DefaultValue(4.11)]
        public double ConceptusEnergyContent_CP8 { get; set; }

        /// <summary>
        /// Conceptus energy parameter (SCA CP9)
        /// </summary>
        [Description("Conceptus weight parameter [CP9]")]
        [System.ComponentModel.DefaultValue(343.5)]
        public double ConceptusEnergyParameter_CP9 { get; set; }

        /// <summary>
        /// Conceptus energy parameter #2 (SCA CP10)
        /// </summary>
        [Description("Conceptus weight parameter 2 [CP10]")]
        [System.ComponentModel.DefaultValue(0.0164)]
        public double ConceptusEnergyParameter2_CP10 { get; set; }

        /// <summary>
        /// Conceptus protein content (SCA CP11)
        /// </summary>
        [Description("Conceptus protein content [CP11]")]
        [System.ComponentModel.DefaultValue(343.5)]
        public double ConceptusProteinContent_CP11 { get; set; }

        /// <summary>
        /// Conceptus protein parameter (SCA CP12)
        /// </summary>
        [Description("Conceptus protein parameter [CP12]")]
        [System.ComponentModel.DefaultValue(343.5)]
        public double ConceptusProteinParameter_CP12 { get; set; }

        /// <summary>
        /// Conceptus protein parameter #2 (SCA CP13)
        /// </summary>
        [Description("Conceptus protein parameter 2 [CP13]")]
        [System.ComponentModel.DefaultValue(343.5)]
        public double ConceptusProteinParameter2_CP13 { get; set; }

        /// <summary>
        /// Fetal growth in poor condition for 1,2,3,... young (SCA CP14)
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Fetal growth in poor condition [CP14]")]
        [Required, MinLength(1)]
        public double[] FetalGrowthPoorCondition_CP14 { get; set; }

        #endregion

        #region add to #

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double RumenDegradableProteinShortfallScalar { get; set; }

        #endregion

        /// <summary>
        /// Mortality rate coefficient
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Mortality rate coefficient")]
        [Required, GreaterThanValue(0)]
        public double MortalityCoefficient { get; set; }
        /// <summary>
        /// Mortality rate intercept
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Mortality rate intercept")]
        [Required, GreaterThanValue(0)]
        public double MortalityIntercept { get; set; }
        /// <summary>
        /// Mortality rate exponent
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Mortality rate exponent")]
        [Required, GreaterThanValue(0)]
        public double MortalityExponent { get; set; }
        /// <summary>
        /// Juvenile mortality rate coefficient
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Juvenile mortality rate coefficient")]
        [Required, GreaterThanValue(0)]
        public double JuvenileMortalityCoefficient { get; set; }
        /// <summary>
        /// Juvenile mortality rate maximum
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Juvenile mortality rate maximum")]
        [Required, Proportion]
        public double JuvenileMortalityMaximum { get; set; }
        /// <summary>
        /// Juvenile mortality rate exponent
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Juvenile mortality rate exponent")]
        [Required]
        public double JuvenileMortalityExponent { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrowSCA()
        {
            this.SetDefaults();
        }

    }
}
