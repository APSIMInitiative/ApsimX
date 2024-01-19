using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the general parameters for a ruminant Type
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("This model provides all general parameters for the RuminantType")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGeneral.htm")]
    public class RuminantParametersGeneral: CLEMModel
    {
        /// <summary>
        /// Name of breed
        /// Name of herd defined by the name of the RuminantType
        /// </summary>
        [Category("Basic", "General")]
        [Description("Breed")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of breed required")]
        public string Breed { get; set; }

        #region Age

        /// <summary>
        /// Natural weaning age
        /// </summary>
        [Category("Basic", "Growth")]
        [Description("Natural weaning age (0 to use gestation length)")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier NaturalWeaningAge { get; set; }

        #endregion

        #region breeding

        /// <summary>
        /// Days between conception and parturition
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Days from conception to parturition")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier GestationLength { get; set; }

        /// <summary>
        /// Number of days for milking
        /// </summary>
        [Category("Basic", "Lactation")]
        [Description("Number of days for milking")]
        [Required, GreaterThanEqualValue(0)]
        public double MilkingDays { get; set; }

        /// <summary>
        /// Peak milk yield(kg/day)
        /// </summary>
        [Category("Basic", "Lactation")]
        [Description("Peak milk yield (kg/day)")]
        [Required, GreaterThanValue(0)]
        public double MilkPeakYield { get; set; }

        #endregion

        #region Size

        /// <summary>
        /// Standard Reference Weight of female
        /// </summary>
        [Category("Basic", "General")]
        [Units("kg")]
        [Description("Standard Ref. Weight (kg) for a female")]
        [Required, GreaterThanValue(0)]
        public double SRWFemale { get; set; }
        /// <summary>
        /// Standard Reference Weight for male from female multiplier
        /// </summary>
        [Category("Advanced", "General")]
        [Description("Male Standard Ref. Weight multiplier from female")]
        [Required, GreaterThanValue(0)]
        public double SRWMaleMultiplier { get; set; }
        /// <summary>
        /// Standard Reference Weight at birth
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Units("Proportion of female SRW")]
        [Description("Birth mass as proportion of female SRW (singlet,twins,triplets..)")]
        [Required, GreaterThanValue(0), Proportion, MinLength(1)]
        public double[] BirthScalar { get; set; }

        /// <summary>
        /// Weight(kg) of 1 animal equivalent (steer)
        /// </summary>
        [Category("Basic", "General")]
        [Description("Weight (kg) of an animal equivalent")]
        [Required, GreaterThanValue(0)]
        public double BaseAnimalEquivalent { get; set; }

        #endregion

        #region Condition

        /// <summary>
        /// Relative body condition to score rate
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Rel. Body Cond. to Score rate")]
        [Required, GreaterThanValue(0)]
        public double RelBCToScoreRate { get; set; } = 0.15;
        /// <summary>
        /// Body condition score range
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Body Condition Score range (min, mid, max)")]
        [Required, ArrayItemCount(3)]
        public double[] BCScoreRange { get; set; } = { 0, 3, 5 };

        #endregion

        #region Mortality

        /// <summary>
        /// Style of calculating condition-based mortality
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Style of calculating additional condition-based mortality")]
        [System.ComponentModel.DefaultValue(ConditionBasedCalculationStyle.None)]
        [Required]
        public ConditionBasedCalculationStyle ConditionBasedMortalityStyle { get; set; }
        /// <summary>
        /// Cut-off for condition-based mortality
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Cut-off for condition-based mortality")]
        [Required]
        public double ConditionBasedMortalityCutOff { get; set; }

        /// <summary>
        /// Probability of dying if less than condition-based mortality cut-off
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Probability of death below condition-based cut-off")]
        [System.ComponentModel.DefaultValue(1)]
        [Required, GreaterThanValue(0), Proportion]
        public double ConditionBasedMortalityProbability { get; set; }

        /// <summary>
        /// Mortality rate base
        /// </summary>
        [Category("Basic", "Survival")]
        [Description("Mortality rate base")]
        [Required, Proportion]
        public double MortalityBase { get; set; }

        #endregion


        #region XXX CN

        /// <summary>
        /// Age growth rate coefficient (CN1 in SCA)
        /// </summary>
        /// <value>Default value for cattle</value>
        [Description("Age growth rate coefficient [CN1]")]
        [System.ComponentModel.DefaultValue(0.0115)]
        public double AgeGrowthRateCoefficient_CN1 { get; set; }

        /// <summary>
        /// Standard Reference Weight growth scalar (CN2 in SCA)
        /// </summary>
        /// <value>Default value for cattle</value>
        [Description("Standard Reference Weight growth scalar [CN2]")]
        [System.ComponentModel.DefaultValue(0.27)]
        public double SRWGrowthScalar_CN2 { get; set; }

        /// <summary>
        /// Slow growth factor (CN3 in SCA)
        /// </summary>
        /// <value>Default value for cattle</value>
        [Description("Slow growth factor [CN3]")]
        [System.ComponentModel.DefaultValue(0.4)]
        public double SlowGrowthFactor_CN3 { get; set; }

        #endregion

        #region Other

        /// <summary>
        /// Methane production from intake coefficient
        /// </summary>
        [Category("Advanced", "Products")]
        [Description("Methane production from intake coefficient")]
        [Required, GreaterThanValue(0)]
        public double MethaneProductionCoefficient { get; set; }

        #endregion




        /// <summary>
        /// Constructor to set defaults when needed
        /// </summary>
        public RuminantParametersGeneral()
        {
            base.SetDefaults();
        }
    }
}
