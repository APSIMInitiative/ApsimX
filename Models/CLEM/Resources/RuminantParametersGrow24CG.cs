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
        [System.ComponentModel.DefaultValue(0.6)]
        [Required, Proportion]
        public double EfficiencyOfDPLSUseForWool_CG1 { get; set; }

        /// <summary>
        /// Efficiency of DPLS use from Feed (CG2 in SCA) [Breed] - [0.7] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Efficiency of DPLS use from feed [CG2]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.7)]
        [Required, Proportion]
        public double EfficiencyOfDPLSUseFromFeed_CG2 { get; set; }

        /// <summary>
        /// Efficiency of DPLS use from milk (CG3 in SCA) [Breed] - [0.8] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Efficiency of DPLS use from milk [CG3]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.8)]
        [Required, Proportion]
        public double EfficiencyOfDPLSUseFromMilk_CG3 { get; set; }

        /// <summary>
        /// Gain curvature (CG4 in SCA) [breed] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Gain curvature [CG4]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(6.0)]
        [Required]
        public double GainCurvature_CG4 { get; set; }

        /// <summary>
        /// Gain midpoint (CG5 in SCA) [breed] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Gain midpoint [CG5]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.4)]
        [Required]
        public double GainMidpoint_CG5 { get; set; }

        /// <summary>
        /// Condition no effect (CG6 in SCA)  [breed] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Condition no effect [CG6]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.9)]
        [Required]
        public double ConditionNoEffect_CG6 { get; set; }

        /// <summary>
        /// Condition maximum effect (CG7 in SCA) [breed] - Growth
        /// </summary>
        /// <value>Default is for cattle</value>
        [Description("Condition max effect [CG7]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.97)]
        [Required]
        public double ConditionMaxEffect_CG7 { get; set; }

        /// <summary>
        /// Intercept parameter for calculation of energy needed per kg empty body gain #1 (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG8)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy per kg growth #1 [CG8]")]
        [System.ComponentModel.DefaultValue(27.0)] // B.indicus 23.2
        [Required, GreaterThanValue(0)] 
        public double GrowthEnergyIntercept1_CG8 { get; set; }

        /// <summary>
        /// Intercept Parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG9)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy per kg growth #2 [CG9]")]
        [System.ComponentModel.DefaultValue(20.3)] // B.indicus 16.5
        [Required, GreaterThanValue(0)] 
        public double GrowthEnergyIntercept2_CG9 { get; set; }

        /// <summary>
        /// Slope parameter for calculation of energy needed per kg empty body gain #1 (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG10)
        /// </summary>
        /// <values>Default is for cattle</values>
        [Category("Breed", "Growth")]
        [Description("Growth energy slope #1 [CG10]")]
        [System.ComponentModel.DefaultValue(2.0)]
        [Required, GreaterThanValue(0)] 
        public double GrowthEnergySlope1_CG10 { get; set; }

        /// <summary>
        /// Slope parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants, SCA CG11)
        /// </summary>
        /// <values>Default is for cattle (20.3), Bos indicus breed value used</values>
        [Category("Breed", "Growth")]
        [Description("Energy per kg growth #2 [CG11]")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(13.8)]
        public double GrowthEnergySlope2_CG11 { get; set; }

        /// <summary>
        /// First intercept of equation to determine energy protein mass (kg kg-1, SCA CG12)
        /// </summary>
        [Description("Protein gain intercept #1 [CG12]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.072)] // B.indicus 0.092 
        public double ProteinGainIntercept1_CG12 { get; set; }

        /// <summary>
        /// Second intercept of equation to determine energy protein mass (kg kg-1, SCA CG13)
        /// </summary>
        [Description("Protein gain intercept #2 [CG13]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.140)] // B.indicus 0.120
        public double ProteinGainIntercept2_CG13 { get; set; }

        /// <summary>
        /// First slope of equation to determine energy protein mass (kg kg-1, SCA CG14)
        /// </summary>
        /// <values>Default is for cattle</values>
        [Description("Protein gain slope #1 [CG14]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.008)]
        public double ProteinGainSlope1_CG14 { get; set; }

        /// <summary>
        /// Second slope of equation to determine energy protein mass (kg kg-1, SCA CG15)
        /// </summary>
        /// <values>Default is for cattle</values>
        [Description("Protein gain slope #2 [CG15]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.115)]
        public double ProteinGainSlope2_CG15 { get; set; }

        /// <summary>
        /// Breed growth efficiency scalar
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Breed growth efficiency scalar")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1)]
        public double BreedGrowthEfficiencyScalar { get; set; }

        /// <summary>
        /// Breed lactation efficiency scalar
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Breed lactation efficiency scalar")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1)]
        public double BreedLactationEfficiencyScalar { get; set; }

        /// <summary>
        /// Breed maintenance efficiency scalar
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Breed maintenance efficiency scalar")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1)]
        public double BreedMainenanceEfficiencyScalar { get; set; }

        #endregion


        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrow24CG()
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
