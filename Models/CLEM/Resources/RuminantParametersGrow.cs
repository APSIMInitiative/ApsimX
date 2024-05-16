using Models.CLEM.Interfaces;
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
    [Description("This model provides all parameters specific to RuminantActivityGrow (V1 CLEM)")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGrow: CLEMModel, ISubParameters, ICloneable
    {
        #region Mortality

        /// <summary>
        /// Base mortality rate (annual)
        /// </summary>
        [Category("Farm", "Survival")]
        [Description("Base mortality rate (annual)")]
        [Required, Proportion]
        [System.ComponentModel.DefaultValue(0.03)]
        public double MortalityBase { get; set; }

        /// <summary>
        /// Daily base mortality rate
        /// </summary>
        public double MortalityBaseDaily { get { return MortalityBase / 365.0; } }

        #endregion

        /// <summary>
        /// Parameter for calculation of energy needed per kg empty body gain #1 (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy per kg growth #1")]
        [Required, GreaterThanValue(0)]
        public double GrowthEnergyIntercept1 { get; set; } = 6.7;
        /// <summary>
        /// Parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy per kg growth #2")]
        [Required, GreaterThanValue(0)]
        public double GrowthEnergyIntercept2 { get; set; } = 20.3;

        /// <summary>
        /// Energy maintenance efficiency coefficient
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy maintenance efficiency coefficient")]
        [Required, GreaterThanValue(0)]
        public double EMaintEfficiencyCoefficient { get; set; } = 0.368;
        
        /// <summary>
        /// Energy maintenance efficiency intercept
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy maintenance efficiency intercept")]
        [Required, GreaterThanValue(0)]
        public double EMaintEfficiencyIntercept { get; set; } = 0.503;
        
        /// <summary>
        /// Energy growth efficiency coefficient
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy growth efficiency coefficient")]
        [Required, GreaterThanValue(0)]
        public double EGrowthEfficiencyCoefficient { get; set; } = 1.16;
        
        /// <summary>
        /// Energy growth efficiency intercept
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy growth efficiency intercept")]
        [Required]
        public double EGrowthEfficiencyIntercept { get; set; } = -0.308;
        
        /// <summary>
        /// Energy lactation efficiency coefficient
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy lactation efficiency coefficient")]
        [Required, GreaterThanValue(0)]
        public double ELactationEfficiencyCoefficient { get; set; } = 0.35;
        
        /// <summary>
        /// Energy lactation efficiency intercept
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy lactation efficiency intercept")]
        [Required, GreaterThanValue(0)]
        public double ELactationEfficiencyIntercept { get; set; } = 0.42;
        
        /// <summary>
        /// Energy maintenance exponent
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy maintenance exponent")]
        [Required, GreaterThanValue(0)]
        public double EMaintExponent { get; set; } = 8.2E-05;
        
        /// <summary>
        /// Energy maintenance intercept
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy maintenance intercept")]
        [Required, GreaterThanValue(0)]
        public double EMaintIntercept { get; set; } = 0.09;
        
        /// <summary>
        /// Energy maintenance coefficient
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Energy maintenance coefficient")]
        [Required, GreaterThanValue(0)]
        public double EMaintCoefficient { get; set; } = 0.26;
        
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
        public double Kme { get; set; } = 1.2;

        /// <summary>
        /// Growth efficiency
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Growth efficiency")]
        [Required, GreaterThanValue(0)]
        public double GrowthEfficiency { get; set; } = 1.0;

        /// <summary>
        /// Intake coefficient in relation to live weight
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Intake coefficient in relation to Live Weight")]
        [Required, GreaterThanValue(0)]
        public double IntakeCoefficient { get; set; } = 0.02425;
        /// <summary>
        /// Intake intercept in relation to live weight
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Intake intercept in relation to SRW")]
        [Required, GreaterThanValue(0)]
        public double IntakeIntercept { get; set; } = 1.7;

        /// <summary>
        /// Protein requirement coeff (g/kg feed)
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Protein requirement coeff (g/kg feed)")]
        [Required, GreaterThanValue(0)]
        public double ProteinCoefficient { get; set; } = 130;
        /// <summary>
        /// Protein degradability
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Protein degradability")]
        [Required, GreaterThanValue(0)]
        public double ProteinDegradability { get; set; } = 0.9;

        /// <summary>
        /// Coefficient of juvenile milk intake
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Coefficient of juvenile milk intake")]
        [Required, GreaterThanValue(0)]
        public double MilkIntakeCoefficient { get; set; } = 0.1206;
        
        /// <summary>
        /// Intercept of juvenile milk intake
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Intercept of juvenile milk intake")]
        [Required, GreaterThanValue(0)]
        public double MilkIntakeIntercept { get; set; } = 3.8146;
        
        /// <summary>
        /// Maximum juvenile milk intake
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Maximum juvenile milk intake")]
        [Required, GreaterThanValue(0)]
        public double MilkIntakeMaximum { get; set; } = 20;
        
        /// <summary>
        /// Milk as proportion of LWT for fodder substitution
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Milk as proportion of LWT for fodder substitution")]
        [Required, Proportion]
        public double MilkLWTFodderSubstitutionProportion { get; set; } = 0.2;
        
        /// <summary>
        /// Max juvenile (suckling) intake as proportion of LWT
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Max juvenile (suckling) intake as proportion of LWT")]
        [Required, GreaterThanValue(0)]
        public double MaxJuvenileIntake { get; set; } = 0.03;
        
        /// <summary>
        /// Proportional discount to intake due to milk intake
        /// </summary>
        [Category("Breed", "Diet")]
        [Description("Proportional discount to intake due to milk intake")]
        [Required, Proportion]
        public double ProportionalDiscountDueToMilk { get; set; } = 0.3;

        /// <summary>
        /// Maximum size of individual relative to SRW
        /// </summary>
        [Category("Farm", "General")]
        [Description("Maximum size of individual relative to SRW")]
        [Required, GreaterThanValue(0)]
        public double MaximumSizeOfIndividual { get; set; } = 1.1;

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
        public double LactatingPotentialModifierConstantA { get; set; } = 0.32;
        
        /// <summary>
        /// Lactating Potential intake modifier Coefficient B
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Lactating potential intake modifier coefficient B")]
        [Required, GreaterThanValue(0)]
        public double LactatingPotentialModifierConstantB { get; set; } = 61;
        
        /// <summary>
        /// Lactating Potential intake modifier Coefficient C
        /// </summary>
        [Category("Breed", "Lactation")]
        [Description("Lactating potential intake modifier coefficient C")]
        [Required, GreaterThanValue(0)]
        public double LactatingPotentialModifierConstantC { get; set; } = 1.7;



        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantParametersGrow()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Create a copy of the class
        /// </summary>
        /// <returns>A new RuminantParametersGrow</returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            RuminantParametersGrow clonedParameters = new()
            {
                CashmereCoefficient = CashmereCoefficient,
                EGrowthEfficiencyCoefficient = EGrowthEfficiencyCoefficient,
                EGrowthEfficiencyIntercept = EGrowthEfficiencyIntercept,
                ELactationEfficiencyCoefficient = ELactationEfficiencyCoefficient,
                ELactationEfficiencyIntercept = ELactationEfficiencyIntercept,
                EMaintCoefficient = EMaintCoefficient,
                EMaintEfficiencyCoefficient = EMaintEfficiencyCoefficient,
                EMaintEfficiencyIntercept = EMaintEfficiencyIntercept,
                EMaintExponent = EMaintExponent,
                EMaintIntercept = EMaintIntercept,
                EnergyMaintenanceMaximumAge = EnergyMaintenanceMaximumAge.Clone() as AgeSpecifier,
                GrowthEfficiency = GrowthEfficiency,
                GrowthEnergyIntercept1 = GrowthEnergyIntercept1,
                GrowthEnergyIntercept2 = GrowthEnergyIntercept2,
                IntakeCoefficient = IntakeCoefficient,
                IntakeIntercept = IntakeIntercept,
                MaximumSizeOfIndividual = MaximumSizeOfIndividual,
                MaxJuvenileIntake = MaxJuvenileIntake,
                MilkIntakeCoefficient = MilkIntakeCoefficient,
                MilkIntakeIntercept = MilkIntakeIntercept,
                MilkIntakeMaximum = MilkIntakeMaximum,
                MilkLWTFodderSubstitutionProportion = MilkLWTFodderSubstitutionProportion,
                MortalityBase = MortalityBase,
                Kme = Kme,
                LactatingPotentialModifierConstantA = LactatingPotentialModifierConstantA,
                LactatingPotentialModifierConstantB = LactatingPotentialModifierConstantB,
                LactatingPotentialModifierConstantC = LactatingPotentialModifierConstantC,
                ProportionalDiscountDueToMilk = ProportionalDiscountDueToMilk,
                ProteinCoefficient = ProteinCoefficient,
                ProteinDegradability = ProteinDegradability,
                WoolCoefficient = WoolCoefficient,
            };
            return clonedParameters;
        }
    }
}
