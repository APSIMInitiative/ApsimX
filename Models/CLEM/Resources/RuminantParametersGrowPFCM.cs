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
    /// This stores the parameters relating to RuminantActivityGrowPF for a ruminant Type (CG - Growth parameters)
    /// All default values are provided for Bos taurus cattle with Bos indicus values provided as a comment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersGrowPF))]
    [Description("RuminantActivityGrowPF (CM - metabolism)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrowPFCM.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrowPFCM : CLEMModel, ISubParameters, ICloneable
    {
        #region Metabolism CM#

        /// <summary>
        /// Heat production viscera feed level (CM1 in SCA)
        /// </summary>
        [Category("Core", "Growth")]
        [Description("Heat production viscera feed level [CM1]")]
        public double HPVisceraFL_CM1 { get; set; } = 0.09;

        /// <summary>
        /// Feed heat production scalar (CM2 in SCA)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Feed heat production scalar [CM2]")]
        public double FHPScalar_CM2 { get; set; } = 0.36; // B.indicus 0.31

        /// <summary>
        /// Maintenance exponent for age (SCA CM3)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Maintenance exponent for age [CM3]")]
        public double MainExponentForAge_CM3 { get; set; } = 8e-5;

        /// <summary>
        /// Age effect min (SCA CM4)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Age effect min [CM4]")]
        public double AgeEffectMin_CM4 { get; set; } = 0.84;

        /// <summary>
        /// Milk scalar (SCA CM5)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Milk scalar [CM5]")]
        public double MilkScalar_CM5 { get; set; } = 0.23;

        // Not in SCA tables. CLEM work around
        // all converted into grazing energy factor in grazing.
        /// <summary>
        /// Grazing energy as proportion of metabolic energy
        /// </summary>
        [Description("Grazing energy from metabolic scalar [CM6]")]
        [Category("Breed", "Growth")]
        public double GrazingEnergyFromMetabolicScalar_CM6 { get; set; } = 0.0025;

        // chewing scalar CM6

        // digestibility on chewing CM7

        // Walking Slope CM8

        // Walking intercept CM9

        // Solid diet EFP CM10 - hard coded

        // milk diet EFP CM11 - hard coded
        /// <summary>
        /// Grazing energy as proportion of metabolic energy
        /// </summary>
        [Description("Endogenous Fecal Protein from milk diet [CM11]")]
        [Category("Breed", "Growth")]
        public double EFPFromMilkDiet_CM11 { get; set; } = 5.26e-4;

        /// <summary>
        /// Breed EUP Factor #1 (SCA CM12)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Breed EUP Factor #1 [CM12]")]
        public double BreedEUPFactor1_CM12 { get; set; } = 1.61e-2;  // B.indicus 1.29e-2

        /// <summary>
        /// Breed EUP Factor #2 (SCA CM13)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Breed EUP Factor #2 [CM13]")]
        public double BreedEUPFactor2_CM13 { get; set; } = 4.22e-2; // B.indicus 3.38e-2

        /// <summary>
        /// Dermal loss (SCA CM14)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Dermal loss [CM14]")]
        public double DermalLoss_CM14 { get; set; } = 1.1e-4;

        // sme CM15 - hard coded

        // energy cost walking CM16

        // threshold stocking density CM17

        #endregion

        /// <summary>
        /// Create copy of this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            RuminantParametersGrowPFCM clonedParameters = new()
            {
                HPVisceraFL_CM1 = HPVisceraFL_CM1,
                FHPScalar_CM2 = FHPScalar_CM2,
                MainExponentForAge_CM3 = MainExponentForAge_CM3,
                AgeEffectMin_CM4 = AgeEffectMin_CM4,
                MilkScalar_CM5 = MilkScalar_CM5,
                GrazingEnergyFromMetabolicScalar_CM6 = GrazingEnergyFromMetabolicScalar_CM6,
                EFPFromMilkDiet_CM11 = EFPFromMilkDiet_CM11,
                BreedEUPFactor1_CM12 = BreedEUPFactor1_CM12,
                BreedEUPFactor2_CM13 = BreedEUPFactor2_CM13,
                DermalLoss_CM14 = DermalLoss_CM14
            };
            return clonedParameters;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">");
            htmlWriter.Write("Ruminant parameters for metabolism as used in RuminantActivityGrowPF</div>");
            return htmlWriter.ToString();
        }

        #endregion

    }
}
