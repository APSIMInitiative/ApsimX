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
    [Description("RuminantActivityGrow24 (CRD - rumen digestability)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow24CRD.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrow24CACRD : CLEMModel, ISubParameters, ICloneable
    {
        #region Rumen Degradability CRD#

        /// <summary>
        /// Rumen degradability intercept (SCA CRD1) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradability intercept [CRD1]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.3)]
        public double RumenDegradabilityIntercept_CRD1 { get; set; }

        /// <summary>
        /// Rumen degradability slope (SCA CRD2) (SCA CRD1) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradability slope [CRD2]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.25)]
        public double RumenDegradabilitySlope_CRD2 { get; set; }

        /// <summary>
        /// Rumen degradability slope for concentrates/supplements (SCA CRD3) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradability slope for concentrates [CRD3]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.1)]
        public double RumenDegradabilityConcentrateSlope_CRD3 { get; set; }

        /// <summary>
        /// Rumen degradable protein intercept (SCA CRD4) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradable protein intercept [CRD4]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.007)]
        public double RumenDegradableProteinIntercept_CRD4 { get; set; }

        /// <summary>
        /// Rumen degradable protein slope (SCA CRD5) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradable protein slope [CRD5]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.005)]
        public double RumenDegradableProteinSlope_CRD5 { get; set; }

        /// <summary>
        /// Rumen degradable protein exponent (SCA CRD6) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradable protein exponent [CRD6]")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.35)]
        public double RumenDegradableProteinExponent_CRD6 { get; set; }

        // rumenDegradableProteinTimeOfYear [CRD7] 0.1 - not used

        /// <summary>
        /// Proportion of protein requirement shortfall overcome by recycling to rumen scalar (for tropical breeds)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("N recycling to rumen scalar")]
        [Required, GreaterThanEqualValue(0)]
        [System.ComponentModel.DefaultValue(0.0)] // B.indicus 0.5, B.indicus x breeds 0.25
        public double ProteinShortfallAlleviationScalar { get; set; }

        #endregion

        #region CA#

        // CA1- CA4, CA9 hard coded in DUDP calculations FoodResourceStore.DUDP

        /// <summary>
        /// Milk protein digestability (SCA CA5) [Core] - lactation
        /// </summary>
        [Description("Milk protein digestability [CA5]")]
        [System.ComponentModel.DefaultValue(0.92)]
        [Category("Core", "Growth")]
        [Required, Proportion]
        public double MilkProteinDigestability_CA5 { get; set; }

        /// <summary>
        /// Digestability of microbial protein (SCA CA7) [Core] - growth 
        /// </summary>
        [Description("Digestability of microbial protein [CA7]")]
        [Category("Core", "Growth")]
        [System.ComponentModel.DefaultValue(0.6)]
        [Required, Proportion]
        public double MicrobialProteinDigestibility_CA7 { get; set; }

        /// <summary>
        /// Faecal protein from MCP (SCA CA8) [Core] - growth
        /// </summary>
        [Description("Faecal protein from MCP [CA8]")]
        [Category("Core", "Growth")]
        [System.ComponentModel.DefaultValue(0.25)]
        [Required, Proportion]
        public double FaecalProteinFromMCP_CA8 { get; set; }

        // UDP digestibility in concentrates

        #endregion


        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrow24CACRD()
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
            RuminantParametersGrow24CACRD clonedParameters = new()
            {
                RumenDegradabilityIntercept_CRD1 = RumenDegradabilityIntercept_CRD1,
                RumenDegradabilitySlope_CRD2 = RumenDegradabilitySlope_CRD2,
                RumenDegradabilityConcentrateSlope_CRD3 = RumenDegradabilityConcentrateSlope_CRD3,
                RumenDegradableProteinIntercept_CRD4 = RumenDegradableProteinIntercept_CRD4,
                RumenDegradableProteinSlope_CRD5 = RumenDegradableProteinSlope_CRD5,
                RumenDegradableProteinExponent_CRD6 = RumenDegradableProteinExponent_CRD6,
                ProteinShortfallAlleviationScalar = ProteinShortfallAlleviationScalar,
                MilkProteinDigestability_CA5 = MilkProteinDigestability_CA5,
                MicrobialProteinDigestibility_CA7 = MicrobialProteinDigestibility_CA7,
                FaecalProteinFromMCP_CA8 = FaecalProteinFromMCP_CA8,
            };
            return clonedParameters;
        }
    }
}
