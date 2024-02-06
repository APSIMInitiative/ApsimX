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
    public class RuminantParametersGrow: CLEMModel
    {
        #region Mortality

        /// <summary>
        /// Style of calculating condition-based mortality
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Style of calculating additional condition-based mortality")]
        [System.ComponentModel.DefaultValue(ConditionBasedCalculationStyle.None)]
        [Required]
        public ConditionBasedCalculationStyle ConditionBasedMortalityStyle { get; set; }

        /// <summary>
        /// Cut-off for condition-based mortality
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Cut-off for condition-based mortality")]
        [Required]
        public double ConditionBasedMortalityCutOff { get; set; }

        /// <summary>
        /// Probability of dying if less than condition-based mortality cut-off
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Probability of death below condition-based cut-off")]
        [System.ComponentModel.DefaultValue(1)]
        [Required, GreaterThanValue(0), Proportion]
        public double ConditionBasedMortalityProbability { get; set; }

        /// <summary>
        /// Base mortality rate (annual)
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Base mortality rate (annual)")]
        [Required, Proportion]
        [System.ComponentModel.DefaultValue(0.03)]
        public double MortalityBase { get; set; }

        #endregion

        /// <summary>
        /// Parameter for calculation of energy needed per kg empty body gain #1 (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy per kg growth #1")]
        [Required, GreaterThanValue(0)]
        public double GrowthEnergyIntercept1 { get; set; }
        /// <summary>
        /// Parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy per kg growth #2")]
        [Required, GreaterThanValue(0)]
        public double GrowthEnergyIntercept2 { get; set; }

        /// <summary>
        /// Milk curve shape suckling
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk curve shape suckling")]
        [Required, GreaterThanValue(0)]
        public double MilkCurveSuckling { get; set; }
        /// <summary>
        /// Milk curve shape non suckling
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk curve shape non suckling")]
        [Required, GreaterThanValue(0)]
        public double MilkCurveNonSuckling { get; set; }
        /// <summary>
        /// Milk offset day
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Milk offset day")]
        [Required, GreaterThanValue(0)]
        public double MilkOffsetDay { get; set; }
        /// <summary>
        /// Milk peak day
        /// </summary>
        [Category("Farm", "Lactation")]
        [Description("Milk peak day")]
        [Required, GreaterThanValue(0)]
        public double MilkPeakDay { get; set; }

        /// <summary>
        /// Energy maintenance efficiency coefficient
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy maintenance efficiency coefficient")]
        [Required, GreaterThanValue(0)]
        public double EMaintEfficiencyCoefficient { get; set; }
        /// <summary>
        /// Energy maintenance efficiency intercept
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy maintenance efficiency intercept")]
        [Required, GreaterThanValue(0)]
        public double EMaintEfficiencyIntercept { get; set; }
        /// <summary>
        /// Energy growth efficiency coefficient
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy growth efficiency coefficient")]
        [Required, GreaterThanValue(0)]
        public double EGrowthEfficiencyCoefficient { get; set; }
        /// <summary>
        /// Energy growth efficiency intercept
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy growth efficiency intercept")]
        [Required]
        public double EGrowthEfficiencyIntercept { get; set; }
        /// <summary>
        /// Energy lactation efficiency coefficient
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy lactation efficiency coefficient")]
        [Required, GreaterThanValue(0)]
        public double ELactationEfficiencyCoefficient { get; set; }
        /// <summary>
        /// Energy lactation efficiency intercept
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy lactation efficiency intercept")]
        [Required, GreaterThanValue(0)]
        public double ELactationEfficiencyIntercept { get; set; }
        /// <summary>
        /// Energy maintenance exponent
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy maintenance exponent")]
        [Required, GreaterThanValue(0)]
        public double EMaintExponent { get; set; }
        /// <summary>
        /// Energy maintenance intercept
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy maintenance intercept")]
        [Required, GreaterThanValue(0)]
        public double EMaintIntercept { get; set; }
        /// <summary>
        /// Energy maintenance coefficient
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy maintenance coefficient")]
        [Required, GreaterThanValue(0)]
        public double EMaintCoefficient { get; set; }
        /// <summary>
        /// Maximum age for energy maintenance calculation
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Maximum age for energy maintenance calculation")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier EnergyMaintenanceMaximumAge { get; set; } = new int[] { 6, 0, 0 };

        /// <summary>
        /// Breed factor for maintenence energy
        /// </summary>
        [Category("Basic", "Growth")]
        [Description("Breed factor for maintenence energy")]
        [Required, GreaterThanValue(0)]
        public double Kme { get; set; }

        /// <summary>
        /// Growth efficiency
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Growth efficiency")]
        [Required, GreaterThanValue(0)]
        public double GrowthEfficiency { get; set; }

        /// <summary>
        /// Intake coefficient in relation to live weight
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Intake coefficient in relation to Live Weight")]
        [Required, GreaterThanValue(0)]
        public double IntakeCoefficient { get; set; }
        /// <summary>
        /// Intake intercept in relation to live weight
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Intake intercept in relation to SRW")]
        [Required, GreaterThanValue(0)]
        public double IntakeIntercept { get; set; }

        /// <summary>
        /// Protein requirement coeff (g/kg feed)
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Protein requirement coeff (g/kg feed)")]
        [Required, GreaterThanValue(0)]
        public double ProteinCoefficient { get; set; }
        /// <summary>
        /// Protein degradability
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Protein degradability")]
        [Required, GreaterThanValue(0)]
        public double ProteinDegradability { get; set; }

                /// <summary>
        /// Coefficient of juvenile milk intake
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Coefficient of juvenile milk intake")]
        [Required, GreaterThanValue(0)]
        public double MilkIntakeCoefficient { get; set; }
        /// <summary>
        /// Intercept of juvenile milk intake
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Intercept of juvenile milk intake")]
        [Required, GreaterThanValue(0)]
        public double MilkIntakeIntercept { get; set; }
        /// <summary>
        /// Maximum juvenile milk intake
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Maximum juvenile milk intake")]
        [Required, GreaterThanValue(0)]
        public double MilkIntakeMaximum { get; set; }
        /// <summary>
        /// Milk as proportion of LWT for fodder substitution
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Milk as proportion of LWT for fodder substitution")]
        [Required, Proportion]
        public double MilkLWTFodderSubstitutionProportion { get; set; }
        /// <summary>
        /// Max juvenile (suckling) intake as proportion of LWT
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Max juvenile (suckling) intake as proportion of LWT")]
        [Required, GreaterThanValue(0)]
        public double MaxJuvenileIntake { get; set; }
        /// <summary>
        /// Proportional discount to intake due to milk intake
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Proportional discount to intake due to milk intake")]
        [Required, Proportion]
        public double ProportionalDiscountDueToMilk { get; set; }

        /// <summary>
        /// Maximum size of individual relative to SRW
        /// </summary>
        [Category("Farm", "General")]
        [Description("Maximum size of individual relative to SRW")]
        [Required, GreaterThanValue(0)]
        public double MaximumSizeOfIndividual { get; set; }

        /// <summary>
        /// Wool coefficient
        /// </summary>
        [Category("Breed", "Products")]
        [Description("Wool coefficient")]
        [Required]
        public double WoolCoefficient { get; set; }

        /// <summary>
        /// Cashmere coefficient
        /// </summary>
        [Category("Breed", "Products")]
        [Description("Cashmere coefficient")]
        [Required]
        public double CashmereCoefficient { get; set; }

        /// <summary>
        /// Lactating Potential intake modifier Coefficient A
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Lactating potential intake modifier coefficient A")]
        [Required, GreaterThanValue(0)]
        public double LactatingPotentialModifierConstantA { get; set; }
        /// <summary>
        /// Lactating Potential intake modifier Coefficient B
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Lactating potential intake modifier coefficient B")]
        [Required, GreaterThanValue(0)]
        public double LactatingPotentialModifierConstantB { get; set; }
        /// <summary>
        /// Lactating Potential intake modifier Coefficient C
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Lactating potential intake modifier coefficient C")]
        [Required, GreaterThanValue(0)]
        public double LactatingPotentialModifierConstantC { get; set; }

        /// <summary>
        /// Mortality rate coefficient
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Mortality rate coefficient")]
        [Required, GreaterThanValue(0)]
        public double MortalityCoefficient { get; set; }
        /// <summary>
        /// Mortality rate intercept
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Mortality rate intercept")]
        [Required, GreaterThanValue(0)]
        public double MortalityIntercept { get; set; }
        /// <summary>
        /// Mortality rate exponent
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Mortality rate exponent")]
        [Required, GreaterThanValue(0)]
        public double MortalityExponent { get; set; }
        /// <summary>
        /// Juvenile mortality rate coefficient
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Juvenile mortality rate coefficient")]
        [Required, GreaterThanValue(0)]
        public double JuvenileMortalityCoefficient { get; set; }
        /// <summary>
        /// Juvenile mortality rate maximum
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Juvenile mortality rate maximum")]
        [Required, Proportion]
        public double JuvenileMortalityMaximum { get; set; }
        /// <summary>
        /// Juvenile mortality rate exponent
        /// </summary>
        [Category("Breed", "Survival")]
        [Description("Juvenile mortality rate exponent")]
        [Required]
        public double JuvenileMortalityExponent { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrow()
        {
            this.SetDefaults();
        }
    }
}
