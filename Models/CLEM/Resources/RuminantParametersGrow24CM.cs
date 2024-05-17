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
    [Description("RuminantActivityGrow24 (CM - metabolism)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow24CML.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrow24CM : CLEMModel, ISubParameters, ICloneable
    {
        #region Metabolism CM#

        /// <summary>
        /// Heat production viscera feed level (CM1 in SCA)
        /// </summary>
        [Category("Core", "Growth")]
        [Description("Heat production viscera feed level [CM1]")]
        [System.ComponentModel.DefaultValue(0.09)]
        public double HPVisceraFL_CM1 { get; set; }

        /// <summary>
        /// Feed heat production scalar (CM2 in SCA)
        /// </summary>
        /// <value>Default is for cattle. Value for Bos indicus breeds with all other cattle 0.36</value>
        [Category("Breed", "Growth")]
        [Description("Feed heat production scalar [CM2]")]
        [System.ComponentModel.DefaultValue(0.36)] // B indicus 0.31
        public double FHPScalar_CM2 { get; set; }

        /// <summary>
        /// Maintenance exponent for age (SCA CM3)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Maintenance exponent for age [CM3]")]
        [System.ComponentModel.DefaultValue(8e-5)]
        public double MainExponentForAge_CM3 { get; set; }

        /// <summary>
        /// Age effect min (SCA CM4)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Age effect min [CM4]")]
        [System.ComponentModel.DefaultValue(0.84)]
        public double AgeEffectMin_CM4 { get; set; }

        /// <summary>
        /// Milk scalar (SCA CM5)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Milk scalar [CM5]")]
        [System.ComponentModel.DefaultValue(0.23)]
        public double MilkScalar_CM5 { get; set; }

        // Not in SCA tables. CLEM work around
        // all converted into grazing energy factor in grazing.
        /// <summary>
        /// Grazing energy as proportion of metabolic energy
        /// </summary>
        [Description("Grazing energy from metabolic scalar")]
        [Category("Breed", "Growth")]
        [System.ComponentModel.DefaultValue(0.2)]
        public double GrazingEnergyFromMetabolicScalar { get; set; }

        // chewing scalar CM6

        // digestability on chewing CM7

        // Walking Slope CM8

        // Walking intercept CM9

        // Solid diet EFP CM10 - hard coded

        // milk diet EFP CM11 - hard coded

        /// <summary>
        /// Breed EUP Factor #1 (SCA CM12)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Breed EUP Factor #1 [CM12]")]
        [System.ComponentModel.DefaultValue(1.61e-2)] // B.indicus 1.29e-2
        public double BreedEUPFactor1_CM12 { get; set; }

        /// <summary>
        /// Breed EUP Factor #2 (SCA CM13)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Breed EUP Factor #2 [CM13]")]
        [System.ComponentModel.DefaultValue(4.22e-2)] // B.indicus 3.38e-2
        public double BreedEUPFactor2_CM13 { get; set; }

        /// <summary>
        /// Dermal loss (SCA CM14)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Dermal loss [CM14]")]
        [System.ComponentModel.DefaultValue(1.1e-4)]
        public double DermalLoss_CM14 { get; set; }

        // sme CM15 - hard coded

        // energy cost walking CM16

        // threshold stocking density CM17

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrow24CM()
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
            RuminantParametersGrow24CM clonedParameters = new()
            {
                HPVisceraFL_CM1 = HPVisceraFL_CM1,
                FHPScalar_CM2 = FHPScalar_CM2,
                MainExponentForAge_CM3 = MainExponentForAge_CM3,
                AgeEffectMin_CM4 = AgeEffectMin_CM4,
                MilkScalar_CM5 = MilkScalar_CM5,
                GrazingEnergyFromMetabolicScalar = GrazingEnergyFromMetabolicScalar,
                BreedEUPFactor1_CM12 = BreedEUPFactor1_CM12,
                BreedEUPFactor2_CM13 = BreedEUPFactor2_CM13,
                DermalLoss_CM14 = DermalLoss_CM14
            };
            return clonedParameters;
        }
    }
}
