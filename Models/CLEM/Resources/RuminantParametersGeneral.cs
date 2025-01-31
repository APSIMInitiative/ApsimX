using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
    public class RuminantParametersGeneral: CLEMModel, ISubParameters, ICloneable, IValidatableObject
    {
        /// <summary>
        /// Name of breed
        /// </summary>
        /// <remarks>
        /// This rrelates to breed where the name of herd defined by the name of the RuminantType
        /// </remarks>
        [Category("Breed", "General")]
        [Description("Breed")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of breed required")]
        public string Breed { get; set; } = "Bos taurus";

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
        /// Minimum size for 1st mating, proportion of SRW
        /// </summary>
        [Category("Farm", "Breeding")]
        [Description("Female minimum size for 1st mating (prop of SRW)")]
        [Required, Proportion, GreaterThanEqualValue(0.0)]
        public double MinimumSize1stMating { get; set; } = 0.6;

        /// <summary>
        /// Male minimum age for 1st mating
        /// </summary>
        [Category("Farm", "Breeding")]
        [Description("Male minimum age for 1st mating")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier MaleMinimumAge1stMating { get; set; } = new int[] { 24, 0 };

        /// <summary>
        /// Male minimum size for 1st mating, proportion of male SRW
        /// </summary>
        [Category("Farm", "Breeding")]
        [Description("Male minimum size 1st mating (prop of male SRW)")]
        [Required, Proportion, GreaterThanEqualValue(0.0)]
        public double MaleMinimumSize1stMating { get; set; } = 0.6;

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
        public double SRWFemale { get; set; } = 450;

        /// <summary>
        /// Standard Reference Weight for castrated male from female multiplier
        /// </summary>
        [Category("Farm", "General")]
        [Description("Castrated male SRW multiplier from female")]
        [Required, GreaterThanValue(0)]
        public double SRWCastrateMaleMultiplier { get; set; } = 1.2;

        /// <summary>
        /// Standard Reference Weight for male from female multiplier
        /// </summary>
        [Category("Farm", "General")]
        [Description("Male SRW multiplier from female")]
        [Required, GreaterThanValue(0)]
        public double SRWMaleMultiplier { get; set; } = 1.4;

        /// <summary>
        /// Standard Reference Weight at birth
        /// </summary>
        [Category("Breed", "Breeding")]
        [Units("Proportion of female SRW")]
        [Description("Birth mass as proportion of female SRW (singlet,twins,triplets..)")]
        [Required, GreaterThanValue(0), Proportion, MinLength(1)]
        public double[] BirthScalar { get; set; } = new[] { 0.07, 0.055 };

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
        public double BaseAnimalEquivalent { get; set; } = 450;

        #endregion

        #region Condition

        /// <summary>
        /// Relative body condition to score rate
        /// </summary>
        [Category("Farm", "Growth")]
        [Description("Rel. Body Cond. to Score rate")]
        [Required, GreaterThanValue(0)]
        public double RelBCToScoreRate { get; set; } = 0.15;

        /// <summary>
        /// Body condition score range
        /// </summary>
        [Category("Farm", "Growth")]
        [Description("Body Condition Score range (min, mid, max)")]
        [Required, ArrayItemCount(3)]
        public double[] BCScoreRange { get; set; } = new[] { 0.0, 3.0, 5.0 };

        /// <summary>
        /// Starting fat as a proportion of Empty Body Weight assuming Relative Condition of 1. (Mid)
        /// Still growing (e.g. heifers) will be leaner (RC 0.9) and intake males will be leaner based on sex effect (0.85)
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Proportion of EBW fat"), Tooltip("Assumes cow at relative condition 1 (mid condition)")]
        [Required, Proportion, GreaterThanValue(0.0)]
        public double ProportionEBWFat { get; set; } = 0.25;

        /// <summary>
        /// Max fat as a proportion of Empty Body Weight assuming Relative Condition of 1.5
        /// </summary>
        [Category("Breed", "Growth")]
        [Description("Proportion of EBW fat at RC=1.5"), Tooltip("Assumes cow at relative condition 1.5")]
        [Required, Proportion]
        public double ProportionEBWFatMax { get; set; } = 0.45;

        #endregion

        #region Normalised Weight CN

        /// <summary>
        /// Style of obtaining the age growth rate coefficient (CN1)
        /// </summary>
        [Description("Approach used to provide age growth rate coefficient [CN1]")]
        [Category("Farm", "Growth")]
        [Required, GreaterThanValue(0.01)]
        public AgeGrowthRateCoefficientProvisionTypes AgeGrowthRateCoefficientProvisionStyle { get; set; }

        /// <summary>
        /// Age growth rate coefficient (CN1 in SCA)
        /// </summary>
        /// <value>Default value for cattle</value>
        [Description("Age growth rate coefficient [CN1]")]
        [Category("Farm", "Growth")]
        [Required, GreaterThanValue(0)]
        [Core.Display(VisibleCallback = "IsCN1Supplied")]
        public double AgeGrowthRateCoefficient_CN1 { get; set; } = 0.0145; // updated from previous 0.0115 used in IAT/NABSA based on new analysis and breen improvements

        /// <summary>
        /// Average weaning weight to estimate age growth rate coefficient (CN1)
        /// </summary>
        [Description("Average weaning weight to estimate age growth rate coefficient")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        [Units("kg")]
        [Core.Display(VisibleCallback = "IsCN1EstimatedFromWeaningDetails")]
        public double CN1EstimatedWeaningWeight { get; set; }

        /// <summary>
        /// Average weaning weight to estimate age growth rate coefficient (CN1)
        /// </summary>
        [Description("Average weaning weight to estimate age growth rate coefficient")]
        [Category("Breed", "Growth")]
        [Core.Display(SubstituteSubPropertyName = "Parts", VisibleCallback = "IsCN1EstimatedFromWeaningDetails")]
        [Units("years, months, days")]
        public AgeSpecifier CN1EstimatedWeaningAge { get; set; } =  new int[] { 0 };

        /// <summary>
        /// Determines whether CN1 is to be estimated from weaning details supplied
        /// </summary>
        public bool IsCN1EstimatedFromWeaningDetails { get { return AgeGrowthRateCoefficientProvisionStyle == AgeGrowthRateCoefficientProvisionTypes.EstimateFromAverageWeaningDatails; } }

        /// <summary>
        /// Determines whether CN1 is to be estimated from weaning details supplied
        /// </summary>
        public bool IsCN1Supplied { get { return !IsCN1EstimatedFromWeaningDetails;  } }

        /// <summary>
        /// Standard Reference Weight growth scalar (CN2 in SCA)
        /// </summary>
        /// <value>Default value for cattle</value>
        [Description("Standard Reference Weight growth scalar [CN2]")]
        [Category("Breed", "Growth")]
        [Required, GreaterThanValue(0)]
        public double SRWGrowthScalar_CN2 { get; set; } = 0.27;

        /// <summary>
        /// Slow growth factor (CN3 in SCA)
        /// </summary>
        /// <value>Default value for cattle</value>
        [Description("Slow growth factor [CN3]")]
        [Category("Farm", "Growth")]
        [Required, GreaterThanValue(0)]
        public double SlowGrowthFactor_CN3 { get; set; } = 0.4;

        #endregion

        /// <summary>
        /// Conversion from empty body weight to live weight
        /// </summary>
        [Description("Conversion from empty body weight to live weight")]
        [Category("Farm", "Weight")]
        [Required, GreaterThanValue(1.0)]
        public double EBW2LW_CG18 { get; set; } = 1.09;

        /// <summary>
        /// The proportion of SRW empty body weight that is protein
        /// </summary>
        [Description("The proportion of SRW empty body weight that is protein")]
        [Category("Breed", "Weight")]
        [Required, Proportion, GreaterThanValue(0)]
        public double ProportionSRWEmptyBodyProtein { get; set; } = 0.17;

        /// <summary>
        /// Energy content of fat (MJ/kg) (Used in Grow24, SAC07 and Oddy Growth models)
        /// </summary>
        [Description("MJ energy per kg fat")]
        [Category("Core", "Energy")]
        public double MJEnergyPerKgFat { get; set; } = 39.3; //Grow24, 39.6 Oddy;

        /// <summary>
        /// Energy content of protein (MJ/kg) (Used in Grow24, SAC07 and Oddy Growth models)
        /// </summary>
        [Description("MJ energy per kg protein")]
        [Category("Core", "Energy")]
        public double MJEnergyPerKgProtein { get; set; } = 23.6; // Grow24, 23.8 Oddy;

        /// <summary>
        /// Determine whether wool production is included.
        /// </summary>
        [Description("Include wool production")]
        [Category("Breed", "Wool")]
        public bool IncludeWool { get; set; } = false;

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
                MaleMinimumSize1stMating = MaleMinimumSize1stMating,
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

        /// <summary>
        /// A method to calculate the age growth rate coefficient (CN1) from weaning details
        /// </summary>
        public void CalculateAgeGrowthRateCoefficientFromWeaningDetails()
        {
            if (IsCN1EstimatedFromWeaningDetails)
            {
                AgeGrowthRateCoefficient_CN1 = 0;
                if (CN1EstimatedWeaningWeight > 0 && CN1EstimatedWeaningAge.InDays > 0)
                {
                    // this is what GitHub CoPilot suggested before I got the calculation from the JD
                    AgeGrowthRateCoefficient_CN1 = Math.Log(SRWFemale / CN1EstimatedWeaningWeight) / CN1EstimatedWeaningAge.InDays;
                }
            }
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (BirthScalar.Length != MultipleBirthRate.Length - 1)
            {
                yield return new ValidationResult($"The number of [BirthScalar] values [{BirthScalar.Length}] must must be one more than the number of [MultipleBirthRate] values [{MultipleBirthRate.Length}].{Environment.NewLine}Birth rate scalars represent the size at birth relative to female SRW with one value (default) required for singlets and an additional value for each rate provided in [MultipleBirthRate] representing twins, triplets, quadrulpets etc where required.", new string[] { "RuminantParametersGeneral.BirthScalar" });
            }
            if (MaleMinimumAge1stMating.InDays == 0 & MaleMinimumSize1stMating == 0)
            {
                yield return new ValidationResult($"Having both [MaleMinimumAge1stMating] and [MaleMinimumSize1stMating] set to [0] results in an invalid condition where any male is considered mature.{Environment.NewLine}Set at least one of these properties to a value greater than one.", new string[] { "RuminantParametersGeneral.FirstMating" });
            }
            if (MinimumAge1stMating.InDays == 0 & MinimumSize1stMating == 0)
            {
                yield return new ValidationResult($"Having both [MinimumAge1stMating] and [MinimumSize1stMating] set to [0] results in an invalid condition where any female is considered mature.{Environment.NewLine}Set at least one of these properties to a value greater than one.", new string[] { "RuminantParametersGeneral.FirstMating" });
            }
            // estimate from weaning details
            string warnExtra = "Check specified value and contact developers if correct";
            if (IsCN1EstimatedFromWeaningDetails)
            {
                if (CN1EstimatedWeaningWeight <= 0)
                {
                    yield return new ValidationResult($"The [CN1EstimatedWeaningWeight] must be greater than 0", new string[] { "RuminantParametersGeneral.CN1EstimatedWeaningWeight" });
                }
                if (CN1EstimatedWeaningAge.InDays == 0)
                {
                    yield return new ValidationResult($"The [CN1EstimatedWeaningAge] must be greater than 0", new string[] { "RuminantParametersGeneral.CN1EstimatedWeaningAge" });
                }
                warnExtra = $"Check the calculation of AgeGrowthRateCoefficient based on weaning weight [{CN1EstimatedWeaningWeight}] and age [{CN1EstimatedWeaningAge.InDays}] in documentation";
            }
            // ToDo: check the limits for cattle sheep etc
            if (AgeGrowthRateCoefficient_CN1 > 0.02)
            {
                yield return new ValidationResult($"The [AgeGrowthRateCoefficient_CN1] should be less than 0.02{Environment.NewLine}{warnExtra}", new string[] { "RuminantParametersGeneral.AgeGrowthRateCoefficient_CN1" });
            }
            if (AgeGrowthRateCoefficient_CN1 < 0.01)
            {
                yield return new ValidationResult($"The [AgeGrowthRateCoefficient_CN1] should be greater than 0.01{Environment.NewLine}{warnExtra}", new string[] { "RuminantParametersGeneral.AgeGrowthRateCoefficient_CN1" });
            }

        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">");
            htmlWriter.Write("General ruminant parameters used by all activities:</br>");
            htmlWriter.Write($"Standard reference weight (kg) f:{DisplaySummaryValueSnippet<double>(SRWFemale, warnZero: true)} m:{DisplaySummaryValueSnippet<double>(SRWFemale*SRWMaleMultiplier, warnZero: true)} castrate m:{DisplaySummaryValueSnippet<double>(SRWFemale*SRWCastrateMaleMultiplier, warnZero: true)}");
            if (IsCN1EstimatedFromWeaningDetails)
            {
                htmlWriter.Write($"The AgeGrowthRateCoefficient (CN1) is estimated using the average weaning weight of [{CN1EstimatedWeaningWeight}] and [{CN1EstimatedWeaningAge.InDays}] days at weaning.</br>");
            }
            htmlWriter.Write("</div>");
            return htmlWriter.ToString();
        }

        #endregion

    }

    /// <summary>
    /// Styles of estimating the age growth rate coefficient (CN1)
    /// </summary>
    public enum AgeGrowthRateCoefficientProvisionTypes
    {
        /// <summary>
        /// Provide the value to use
        /// </summary>
        ProvideValue,
        /// <summary>
        /// Use average weight and age at weaning to estimate
        /// </summary>
        EstimateFromAverageWeaningDatails
    }
}
