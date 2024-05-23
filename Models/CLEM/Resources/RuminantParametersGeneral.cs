using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
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
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("This model provides all general parameters for the RuminantType")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGeneral.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersGeneral: CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// Name of breed where name of herd defined by the name of the RuminantType
        /// </summary>
        [Category("Breed", "General")]
        [Description("Breed")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of breed required")]
        [System.ComponentModel.DefaultValue("Bos taurus")]
        public string Breed { get; set; }

        /// <summary>
        /// Use corrected equations of animal energy requirement for growth
        /// </summary>
        [Category("Farm", "General")]
        [Description("Use corrected energy equations")]
        public bool UseCorrectedEquations { get; set; } = true;

        #region Age

        /// <summary>
        /// Natural weaning age
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Natural weaning age (0, use gestation length)")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier NaturalWeaningAge { get; set; } = new int[] { 0 };

        #endregion

        #region breeding

        /// <summary>
        /// Female Minimum age for 1st mating
        /// </summary>
        [Category("Farm", "Breeding")]
        [Description("Female minimum age for 1st mating")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier MinimumAge1stMating { get; set; } = new int[] { 24, 0 };

        /// <summary>
        /// Male minimum age for 1st mating
        /// </summary>
        [Category("Farm", "Breeding")]
        [Description("Male minimum age for 1st mating")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier MaleMinimumAge1stMating { get; set; } = new int[] { 24, 0 };

        /// <summary>
        /// Minimum size for 1st mating, proportion of SRW
        /// </summary>
        [Category("Farm", "Breeding")]
        [Description("Minimum size for 1st mating, proportion of SRW")]
        [Required, Proportion, GreaterThanValue(0.0)]
        [System.ComponentModel.DefaultValue(0.6)]
        public double MinimumSize1stMating { get; set; }

        /// <summary>
        /// Days between conception and parturition
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Days from conception to parturition")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier GestationLength { get; set; } = new int[] { 0, 9, 0 };

        #endregion

        #region Size

        /// <summary>
        /// Standard Reference Weight of female
        /// </summary>
        [Category("Farm", "General")]
        [Units("kg")]
        [Description("Standard Ref. Weight for a female")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(450)]
        public double SRWFemale { get; set; }
        
        /// <summary>
        /// Standard Reference Weight for castrated male from female multiplier
        /// </summary>
        [Category("Farm", "General")]
        [Description("Castrated male SRW multiplier from female")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1.2)]
        public double SRWCastrateMaleMultiplier { get; set; }
        
        /// <summary>
        /// Standard Reference Weight for male from female multiplier
        /// </summary>
        [Category("Farm", "General")]
        [Description("Male SRW multiplier from female")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1.4)]
        public double SRWMaleMultiplier { get; set; }
        
        /// <summary>
        /// Standard Reference Weight at birth
        /// </summary>
        [Category("Breed", "Breeding")]
        [Units("Proportion of female SRW")]
        [Description("Birth mass as proportion of female SRW (singlet,twins,triplets..)")]
        [Required, GreaterThanValue(0), Proportion, MinLength(1)]
        [System.ComponentModel.DefaultValue(new [] { 0.07, 0.055 })]
        public double[] BirthScalar { get; set; }

        /// <summary>
        /// Rate at which multiple births are concieved (twins, triplets, ...)
        /// </summary>
        [Category("Breed", "Breeding")]
        [Description("Rate at which multiple births occur (twins,triplets,...")]
        [Proportion]
        public double[] MultipleBirthRate { get; set; } = new double[] { 0.25 };

        /// <summary>
        /// Weight(kg) of 1 animal equivalent (e.g. steer, DSE)
        /// </summary>
        [Category("Farm", "General")]
        [Description("Weight (kg) of an animal equivalent")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(450)]
        public double BaseAnimalEquivalent { get; set; }

        #endregion

        #region Condition

        /// <summary>
        /// Relative body condition to score rate
        /// </summary>
        [Category("Farm", "Growth")]
        [Description("Rel. Body Cond. to Score rate")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(0.15)]
        public double RelBCToScoreRate { get; set; }
        
        /// <summary>
        /// Body condition score range
        /// </summary>
        [Category("Farm", "Growth")]
        [Description("Body Condition Score range (min, mid, max)")]
        [Required, ArrayItemCount(3)]
        [System.ComponentModel.DefaultValue(new[] { 0.0, 3.0, 5.0 })]
        public double[] BCScoreRange { get; set; }

        #endregion

        #region Normalised Weight CN

        /// <summary>
        /// Age growth rate coefficient (CN1 in SCA)
        /// </summary>
        /// <value>Default value for cattle</value>
        [Description("Age growth rate coefficient [CN1]")]
        [System.ComponentModel.DefaultValue(0.0115)]
        [Category("Farm", "Growth")]
        [Required, GreaterThanValue(0)]
        public double AgeGrowthRateCoefficient_CN1 { get; set; }

        /// <summary>
        /// Standard Reference Weight growth scalar (CN2 in SCA)
        /// </summary>
        /// <value>Default value for cattle</value>
        [Description("Standard Reference Weight growth scalar [CN2]")]
        [System.ComponentModel.DefaultValue(0.27)]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        public double SRWGrowthScalar_CN2 { get; set; }

        /// <summary>
        /// Slow growth factor (CN3 in SCA)
        /// </summary>
        /// <value>Default value for cattle</value>
        [Description("Slow growth factor [CN3]")]
        [System.ComponentModel.DefaultValue(0.4)]
        [Category("Farm", "Growth")]
        [Required, GreaterThanValue(0)]
        public double SlowGrowthFactor_CN3 { get; set; }

        #endregion

        /// <summary>
        /// Conversion from empty body weigh to live weight
        /// </summary>
        [Description("Conversion from empty body weigh to live weight")]
        [Category("Farm", "Weight")]
        [Required, GreaterThanValue(1.0)]
        [System.ComponentModel.DefaultValue(1.09)]
        public double EBW2LW_CG18 { get; set; }

        /// <summary>
        /// Constructor to set defaults when needed
        /// </summary>
        public RuminantParametersGeneral()
        {
            base.SetDefaults();
        }

        /// <summary>
        /// Create a clone of this class
        /// </summary>
        /// <returns>A copy of the class</returns>
        public object Clone()
        {
            RuminantParametersGeneral clonedParameters = new()
            {
                NaturalWeaningAge = NaturalWeaningAge.Clone() as AgeSpecifier,
                MinimumAge1stMating = MinimumAge1stMating.Clone() as AgeSpecifier,
                MaleMinimumAge1stMating = MaleMinimumAge1stMating.Clone() as AgeSpecifier,
                MinimumSize1stMating = MinimumSize1stMating,
                GestationLength = GestationLength.Clone() as AgeSpecifier,
                SRWFemale = SRWFemale,
                SRWCastrateMaleMultiplier = SRWCastrateMaleMultiplier,
                SRWMaleMultiplier = SRWMaleMultiplier,
                BirthScalar = BirthScalar.Clone() as double[],
                MultipleBirthRate = MultipleBirthRate.Clone() as double[],
                BaseAnimalEquivalent = BaseAnimalEquivalent,
                RelBCToScoreRate = RelBCToScoreRate,
                BCScoreRange = BCScoreRange.Clone() as double[],
                AgeGrowthRateCoefficient_CN1 = AgeGrowthRateCoefficient_CN1,
                SRWGrowthScalar_CN2 = SRWGrowthScalar_CN2,
                SlowGrowthFactor_CN3 = SlowGrowthFactor_CN3,
            };
            return clonedParameters;
        }

        #region validation

        /// <summary>
        /// Model Validation
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (BirthScalar.Length != MultipleBirthRate.Length - 1)
            {
                string[] memberNames = new string[] { "RuminantType.BirthScalar" };
                results.Add(new ValidationResult($"The number of [BirthScalar] values [{BirthScalar.Length}] must must be one more than the number of [MultipleBirthRate] values [{MultipleBirthRate.Length}].{Environment.NewLine}Birth rate scalars represent the size at birth relative to female SRW with one value (default) required for singlets and an additional value for each rate provided in [MultipleBirthRate] representing twins, triplets, quadrulpets etc where required.", memberNames));
            }
            return results;
        }

        #endregion

    }
}
