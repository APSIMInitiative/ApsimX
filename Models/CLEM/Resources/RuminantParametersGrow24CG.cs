using DocumentFormat.OpenXml.Presentation;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.DCAPST.Environment;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
    [Description("RuminantActivityGrow24 (CG - growth parameters)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow24CG.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrow24CG: CLEMModel, ISubParameters, ICloneable
    {
        #region Growth CG#

        /// <summary>
        /// Efficiency of DPLS use for wool (CG1 in SCA) [Breed] - [0.6] - Wool
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Efficiency of DPLS use for wool [CG1]")]
        [Category("Breed", "Growth")]
        [Required, Proportion]
        public double EfficiencyOfDPLSUseForWool_CG1 { get; set; } = 0.6;

        /// <summary>
        /// Efficiency of DPLS use from Feed (CG2 in SCA) [Breed] - [0.7] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Efficiency of DPLS use from feed [CG2]")]
        [Category("Breed", "Growth")]
        [Required, Proportion]
        public double EfficiencyOfDPLSUseFromFeed_CG2 { get; set; } = 0.7;

        /// <summary>
        /// Efficiency of DPLS use from milk (CG3 in SCA) [Breed] - [0.8] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Efficiency of DPLS use from milk [CG3]")]
        [Category("Breed", "Growth")]
        [Required, Proportion]
        public double EfficiencyOfDPLSUseFromMilk_CG3 { get; set; } = 0.8;

        /// <summary>
        /// Gain curvature (CG4 in SCA) [breed] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Gain curvature [CG4]")]
        [Category("Breed", "Growth")]
        [Required]
        public double GainCurvature_CG4 { get; set; } = 6.0;

        /// <summary>
        /// Gain midpoint (CG5 in SCA) [breed] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Gain midpoint [CG5]")]
        [Category("Breed", "Growth")]
        [Required] public double GainMidpoint_CG5 { get; set; } = 0.4;

        /// <summary>
        /// Condition no effect (CG6 in SCA)  [breed] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Condition no effect [CG6]")]
        [Category("Breed", "Growth")]
        [Required] public double ConditionNoEffect_CG6 { get; set; } = 0.9;

        /// <summary>
        /// Condition maximum effect (CG7 in SCA) [breed] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Condition max effect [CG7]")]
        [Category("Breed", "Growth")]
        [Required] public double ConditionMaxEffect_CG7 { get; set; } = 0.97;

        /// <summary>
        /// Intercept parameter for calculation of energy needed per kg empty body gain #1 
        /// </summary>
        /// <details>
        /// (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG8)
        /// The 'b' inidcates this is a different value to that reported by Freer et al. 2012 due to the corrected reaggangement of equation 104 based on eqn 1.3 in Freer 2007.
        /// This value is used to calculate energyEmptyBodyGain in CalculateEnergy(ind)
        /// </details>
        [Category("Breed", "Growth")]
        [Description("Energy per kg growth #1 [CG8]")]
        [Required, GreaterThanValue(0)] 
        public double GrowthEnergyIntercept1_CG8b { get; set; } = 6.7; //ToDo: check that this is now the same value for B.indicus/Charolais from rearrangement by GrassGro

        /// <summary>
        /// Intercept Parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG9)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy per kg growth #2 [CG9]")]
        [Required, GreaterThanValue(0)] 
        public double GrowthEnergyIntercept2_CG9 { get; set; } = 20.3; // B.indicus 16.5

        /// <summary>
        /// Slope parameter for calculation of energy needed per kg empty body gain #1 (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG10)
        /// </summary>
        /// <values>Default is for cattle</values>
        [Category("Breed", "Growth")]
        [Description("Growth energy slope #1 [CG10]")]
        [Required, GreaterThanValue(0)] 
        public double GrowthEnergySlope1_CG10 { get; set; } = 2.0;

        /// <summary>
        /// Slope parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG11)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy per kg growth #2 [CG11]")]
        [Required, GreaterThanValue(0)]
        public double GrowthEnergySlope2_CG11 { get; set; } = 13.8; 

        /// <summary>
        /// The adjusted intercept of equation to determine energy protein mass (kg kg-1) based on corrected equation to calculate proteinContentOfGain Dougherty 2024, Freer et al., 2012
        /// </summary>
        /// <details>
        /// The 'b' indicates this is a different value to that reported by Freer et al. 2012 due to the corrected reaggangement of equation 105 based on eqn 1.31 in Freer 2007.
        /// This value is used to calculate proteinContentOfGain in CalculateEnergy(ind)
        /// </details>
        [Description("Adjusted Protein gain intercept #1 [CG12]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        public double ProteinGainIntercept1_CG12b { get; set; } = 0.21; // Same for B.indicus 

        /// <summary>
        /// Second intercept of equation to determine energy protein mass (kg kg-1, SCA CG13)
        /// </summary>
        [Description("Protein gain intercept #2 [CG13]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        public double ProteinGainIntercept2_CG13 { get; set; } = 0.140; // B.indicus 0.120

        /// <summary>
        /// First slope of equation to determine energy protein mass (kg kg-1, SCA CG14)
        /// </summary>
        /// <values>Default is for cattle</values>
        [Description("Protein gain slope #1 [CG14]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        public double ProteinGainSlope1_CG14b { get; set; } = 0.004;

        /// <summary>
        /// Second slope of equation to determine energy protein mass (kg kg-1, SCA CG15)
        /// </summary>
        /// <values>Default is for cattle</values>
        [Description("Protein gain slope #2 [CG15]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        public double ProteinGainSlope2_CG15 { get; set; } = 0.115;

        /// <summary>
        /// Breed growth efficiency scalar
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Breed growth efficiency scalar")]
        [Required, GreaterThanValue(0)]
        public double BreedGrowthEfficiencyScalar { get; set; } = 1;

        /// <summary>
        /// Breed lactation efficiency scalar
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Breed lactation efficiency scalar")]
        [Required, GreaterThanValue(0)]
        public double BreedLactationEfficiencyScalar { get; set; } = 1;

        /// <summary>
        /// Breed maintenance efficiency scalar
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Breed maintenance efficiency scalar")]
        [Required, GreaterThanValue(0)]
        public double BreedMainenanceEfficiencyScalar { get; set; } = 1;

        #endregion

        /// <summary>
        /// Create copy of this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            RuminantParametersGrow24CG clonedParameters = new()
            {
                EfficiencyOfDPLSUseForWool_CG1 = EfficiencyOfDPLSUseForWool_CG1,
                EfficiencyOfDPLSUseFromFeed_CG2 = EfficiencyOfDPLSUseFromFeed_CG2,
                EfficiencyOfDPLSUseFromMilk_CG3 = EfficiencyOfDPLSUseFromMilk_CG3,
                GainCurvature_CG4 = GainCurvature_CG4,
                GainMidpoint_CG5 = GainMidpoint_CG5,
                ConditionNoEffect_CG6 = ConditionNoEffect_CG6,
                ConditionMaxEffect_CG7 = ConditionMaxEffect_CG7,
                GrowthEnergyIntercept1_CG8b = GrowthEnergyIntercept1_CG8b,
                GrowthEnergyIntercept2_CG9 = GrowthEnergyIntercept2_CG9,
                GrowthEnergySlope1_CG10 = GrowthEnergySlope1_CG10,
                GrowthEnergySlope2_CG11 = GrowthEnergySlope2_CG11,
                ProteinGainIntercept1_CG12b = ProteinGainIntercept1_CG12b,
                ProteinGainIntercept2_CG13 = ProteinGainIntercept2_CG13,
                ProteinGainSlope1_CG14b = ProteinGainSlope1_CG14b,
                ProteinGainSlope2_CG15 = ProteinGainSlope2_CG15,
                BreedGrowthEfficiencyScalar = BreedGrowthEfficiencyScalar,
                BreedLactationEfficiencyScalar = BreedLactationEfficiencyScalar,
                BreedMainenanceEfficiencyScalar = BreedMainenanceEfficiencyScalar,
            };
            return clonedParameters;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">");
            htmlWriter.Write("Ruminant parameters for growth as used in RuminantActivityGrow24</div>");
            return htmlWriter.ToString();
        }

        #endregion

    }
}
