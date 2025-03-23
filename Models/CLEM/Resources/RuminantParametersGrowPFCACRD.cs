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
    /// This stores the parameters relating to RuminantActivityGrowPF for a ruminant Type (CA - digestibility and CRD - Rumen degradability)
    /// All default values are provided for Bos taurus cattle with Bos indicus values provided as a comment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersGrowPF))]
    [Description("RuminantActivityGrowPF (CRD - rumen digestibility)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrowPFCACRD.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrowPFCACRD : CLEMModel, ISubParameters, ICloneable
    {
        #region Rumen Degradability CRD#

        /// <summary>
        /// Rumen degradability intercept (SCA CRD1) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradability intercept [CRD1]")]
        [Category("Breed", "Growth")]
        public double RumenDegradabilityIntercept_CRD1 { get; set; } = 0.3;

        /// <summary>
        /// Rumen degradability slope (SCA CRD2) (SCA CRD1) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradability slope [CRD2]")]
        [Category("Breed", "Growth")]
        public double RumenDegradabilitySlope_CRD2 { get; set; } = 0.25;

        /// <summary>
        /// Rumen degradability slope for concentrates/supplements (SCA CRD3) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradability slope for concentrates [CRD3]")]
        [Category("Breed", "Growth")]
        public double RumenDegradabilityConcentrateSlope_CRD3 { get; set; } = 0.1;

        /// <summary>
        /// Rumen degradable protein intercept (SCA CRD4) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradable protein intercept [CRD4]")]
        [Category("Breed", "Growth")]
        public double RumenDegradableProteinIntercept_CRD4 { get; set; } = 0.007;

        /// <summary>
        /// Rumen degradable protein slope (SCA CRD5) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradable protein slope [CRD5]")]
        [Category("Breed", "Growth")]
        public double RumenDegradableProteinSlope_CRD5 { get; set; } = 0.005;

        /// <summary>
        /// Rumen degradable protein exponent (SCA CRD6) [Core] [def=] - Growth
        /// </summary>
        [Description("Rumen degradable protein exponent [CRD6]")]
        [Category("Breed", "Growth")]
        public double RumenDegradableProteinExponent_CRD6 { get; set; } = 0.35;

        // rumenDegradableProteinTimeOfYear [CRD7] 0.1 - not used

        /// <summary>
        /// Proportion of protein requirement shortfall overcome by recycling to rumen scalar (for tropical breeds)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("N recycling to rumen scalar")]
        [Required, GreaterThanEqualValue(0)]
        public double ProteinShortfallAlleviationScalar { get; set; } = 0.0; // B.indicus 0.5, B.indicus x breeds 0.25

        #endregion

        #region CA#

        // CA1- CA4, CA9 hard coded in DUDP calculations FoodResourceStore.DUDP

        /// <summary>
        /// Milk protein digestibility (SCA CA5) [Core] - lactation
        /// </summary>
        [Description("Milk protein digestibility [CA5]")]
        [Category("Core", "Growth")]
        [Required, Proportion]
        public double MilkProteinDigestibility_CA5 { get; set; } = 0.92;

        /// <summary>
        /// Digestibility of microbial protein (SCA CA7) [Core] - growth 
        /// </summary>
        [Description("Digestibility of microbial protein [CA7]")]
        [Category("Core", "Growth")]
        [Required, Proportion]
        public double MicrobialProteinDigestibility_CA7 { get; set; } = 0.6;

        /// <summary>
        /// Faecal protein from MCP (SCA CA8) [Core] - growth
        /// </summary>
        [Description("Faecal protein from MCP [CA8]")]
        [Category("Core", "Growth")]
        [Required, Proportion]
        public double FaecalProteinFromMCP_CA8 { get; set; } = 0.25;

        // UDP digestibility in concentrates

        #endregion


        /// <summary>
        /// Create copy of this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            RuminantParametersGrowPFCACRD clonedParameters = new()
            {
                RumenDegradabilityIntercept_CRD1 = RumenDegradabilityIntercept_CRD1,
                RumenDegradabilitySlope_CRD2 = RumenDegradabilitySlope_CRD2,
                RumenDegradabilityConcentrateSlope_CRD3 = RumenDegradabilityConcentrateSlope_CRD3,
                RumenDegradableProteinIntercept_CRD4 = RumenDegradableProteinIntercept_CRD4,
                RumenDegradableProteinSlope_CRD5 = RumenDegradableProteinSlope_CRD5,
                RumenDegradableProteinExponent_CRD6 = RumenDegradableProteinExponent_CRD6,
                ProteinShortfallAlleviationScalar = ProteinShortfallAlleviationScalar,
                MilkProteinDigestibility_CA5 = MilkProteinDigestibility_CA5,
                MicrobialProteinDigestibility_CA7 = MicrobialProteinDigestibility_CA7,
                FaecalProteinFromMCP_CA8 = FaecalProteinFromMCP_CA8,
            };
            return clonedParameters;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">");
            htmlWriter.Write("Ruminant parameters for digestibility (CA) and Rumen degradability (CRD)</div>");
            return htmlWriter.ToString();
        }

        #endregion

    }
}
