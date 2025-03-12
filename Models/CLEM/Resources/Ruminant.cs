using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.Core;
using Models.LifeCycle;
using Models.Logging;
using Models.PMF.Phen;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Transactions;
using System.Xml.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Object for an individual Ruminant Animal.
    /// </summary>
    [Serializable]
    public abstract class Ruminant : IFilterable, IAttributable
    {
        private RuminantFemale mother;
        private int age;
        private bool sterilised = false;

        /// <summary>
        /// Ruminant intake manager
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
        public RuminantIntake Intake { get; set; } = new();

        /// <summary>
        /// Store for tracking energy use
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]

        public RuminantInfoEnergy Energy { get; set; }

        /// <summary>
        /// Store for tracking ruminant outputs
        /// </summary>
        [JsonIgnore]
        public RuminantInfoOutput Output { get; set; } = new RuminantInfoOutput();

        /// <summary>
        /// Store for tracking all weights
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
        public RuminantInfoWeight Weight { get; set; }

        /// <summary>
        /// Current animal price group for this individual 
        /// </summary>
        public (AnimalPriceGroup Buy, AnimalPriceGroup Sell) CurrentPriceGroups { get; set; } = (null, null);

        /// <inheritdoc/>
        public IndividualAttributeList Attributes { get; set; } = new IndividualAttributeList();

        /// <summary>
        /// Report total intake as a proportion of live weight
        /// </summary>
        public double IntakeAsProportionLiveWeight 
        {
            get
            {
                return (Intake.SolidsDaily.Actual + Intake.MilkDaily.Actual) / Weight.Live;
            }
        }

        #region General properties

        /// <summary>
        /// Reference to the Breed Parameters.
        /// </summary>
        public RuminantParameters Parameters;

        /// <summary>
        /// Breed of individual
        /// </summary>
        [FilterByProperty]
        public string Breed { get { return Parameters.General?.Breed ?? "Unknown"; } }

        /// <summary>
        /// Herd individual belongs to
        /// </summary>
        [FilterByProperty]
        public string HerdName { get { return Parameters.Details?.Name ?? "Unknown"; } }

        /// <summary>
        /// Unique ID of individual
        /// </summary>
        [FilterByProperty]
        public int ID { get; set; }

        /// <summary>
        /// Link to individual's mother
        /// </summary>
        public RuminantFemale Mother
        {
            get
            {
                return mother;
            }
            set
            {
                mother = value;
                if (mother != null)
                    MotherID = value.ID;
            }
        }

        /// <summary>
        /// Link to individual's mother
        /// </summary>
        public int MotherID { get; private set; }

        /// <summary>
        /// Individual is suckling, still with mother and not weaned
        /// </summary>
        public bool IsSucklingWithMother { get { return !IsWeaned && mother is not null; } }

        private DateTime dateOfWeaning = default;

        /// <summary>
        /// Weaned individual flag
        /// </summary>
        [FilterByProperty]
        public bool IsWeaned { get { return dateOfWeaning != default; } }

        /// <summary>
        /// Number of days since weaned
        /// </summary>
        [FilterByProperty]
        public int DaysSinceWeaned
        {
            get
            {
                if (IsWeaned)
                    return Convert.ToInt32(DaysSince(RuminantTimeSpanTypes.Weaned, 0.0));
                else
                    return 0;
            }
        }

        /// <summary>
        /// Determine if weaned and less that 12 months old. Weaner
        /// </summary>
        [FilterByProperty]
        public bool IsWeaner
        {
            get
            {
                return (IsWeaned && age < (DateTime.IsLeapYear(DateOfBirth.Year)?366:365));
            }
        }

        /// <summary>
        /// Determine if unweaned suckling
        /// </summary>
        [FilterByProperty]
        public bool IsSuckling
        {
            get
            {
                return !IsWeaned;
            }
        }

        /// <summary>
        /// Number in this class (1 if individual model)
        /// </summary>
        public double Number { get; set; } = 1;

        /// <summary>
        /// Unique ID of the managed paddock the individual is located in.
        /// </summary>
        [FilterByProperty]
        public string Location { get; set; }

        /// <summary>
        /// Indicates if this individual has died before removal from herd
        /// </summary>
        [FilterByProperty]
        public bool Died { get; set; }

        #endregion

        #region Breeding properties

        /// <summary>
        /// Sex of individual
        /// </summary>
        [FilterByProperty]
        public abstract Sex Sex { get; }

        /// <summary>
        /// Sterilise individual
        /// </summary>
        public void Sterilise(bool castration = false)
        {
            sterilised = true;
            if (Sex == Sex.Male && castration)
            {
                Weight.SetStandardReferenceWeight(Parameters.General.SRWFemale * Parameters.General.SRWCastrateMaleMultiplier);
                CalculateNormalisedWeight(age);
            }
        }

        /// <summary>
        /// A state of breeding readiness for reporting
        /// </summary>
        public abstract string BreedingStatus { get; }

        /// <summary>
        /// Has the individual been sterilised (webbed, spayed or castrated)
        /// </summary>
        [FilterByProperty]
        public bool IsSterilised { get { return sterilised; } }

        /// <summary>
        /// Marked as a replacement breeder
        /// </summary>
        [FilterByProperty]
        public bool IsReplacementBreeder { get; set; }

        /// <summary>
        /// Is this individual a valid breeder and in condition
        /// </summary>
        [FilterByProperty]
        public virtual bool IsAbleToBreed { get { return false; } }

        #endregion

        #region Age properties

        /// <summary>
        /// Date of birth
        /// </summary>
        public DateTime DateOfBirth { get; init; }

        private DateTime lastKnownDate = default; 

        /// <summary>
        /// Method to increase age
        /// </summary>
        public void SetCurrentDate(DateTime currentDate)
        {
            lastKnownDate = currentDate;
            AgeInDays = Convert.ToInt32(TimeSince(RuminantTimeSpanTypes.Birth, currentDate).TotalDays);
        }

        /// <summary>
        /// The time-span since the various occasions in individual's life
        /// </summary>
        /// <param name="spanType">The measure to provide</param>
        /// <param name="toDate">Date to calculate to or omit to use date known by individual</param>
        /// <returns>A TimeSpan representing the days since age of the individual</returns>
        public TimeSpan TimeSince(RuminantTimeSpanTypes spanType, DateTime toDate = default)
        {
            DateTime fromDate = default;
            switch(spanType)
            {
                case RuminantTimeSpanTypes.Birth:
                    fromDate = DateOfBirth;
                    break;
                case RuminantTimeSpanTypes.Weaned:
                    if(IsWeaned)
                        fromDate = dateOfWeaning;
                    break;
                case RuminantTimeSpanTypes.Conceived:
                    if (this is RuminantFemale female)
                        fromDate = female.DateLastConceived;
                    break;
                case RuminantTimeSpanTypes.GaveBirth:
                    if (this is RuminantFemale femalebirth)
                        fromDate = femalebirth.DateOfLastBirth;
                    break;
                default:
                    break;
            }

            toDate = toDate == default ? lastKnownDate : toDate;
            if (toDate == default || fromDate == default || toDate < fromDate)
                return TimeSpan.Zero;
            else
                return toDate - fromDate;
        }

        /// <summary>
        /// The number of days since a specified occasion in individual's life
        /// </summary>
        /// <param name="spanType">The measure to provide</param>
        /// <param name="defaultValue">Value to provide when time span cannot be determined</param>
        /// <param name="toDate">Date to calculate to or omit to use date known by individual</param>
        /// <returns>The number of days in the time span</returns>
        public double DaysSince(RuminantTimeSpanTypes spanType, double defaultValue, DateTime toDate = default)
        {
            DateTime fromDate = default;
            switch (spanType)
            {
                case RuminantTimeSpanTypes.Birth:
                    fromDate = DateOfBirth;
                    break;
                case RuminantTimeSpanTypes.Weaned:
                    if (IsWeaned)
                        fromDate = dateOfWeaning;
                    break;
                case RuminantTimeSpanTypes.Conceived:
                    if (this is RuminantFemale female)
                        fromDate = female.DateLastConceived;
                    break;
                case RuminantTimeSpanTypes.GaveBirth:
                    if (this is RuminantFemale femalebirth)
                        fromDate = femalebirth.DateOfLastBirth;
                    break;
                default:
                    break;
            }

            toDate = toDate == default ? lastKnownDate : toDate;
            if (toDate == default || fromDate == default || toDate < fromDate)
                return defaultValue;
            else
                return (toDate - fromDate).TotalDays;
        }

        /// <summary>
        /// Age (Years) estimated assuming 365.2425 days per year
        /// </summary>
        /// <units>Years</units>
        [FilterByProperty]
        public double AgeInYears
        {
            get
            {
                return AgeInDays / 365.2425;
            }
        }

        /// <summary>
        /// Age (whole years)
        /// </summary>
        /// <units>Whole years</units>
        [FilterByProperty]
        public int AgeInWholeYears
        {
            get
            {
                return Convert.ToInt32(Math.Floor(AgeInYears));
            }
        }

        /// <summary>
        /// Age (days)
        /// </summary>
        /// <units>Days</units>
        [FilterByProperty]
        public int AgeInDays
        {
            get
            {
                return age;
            }
            private set
            {
                age = value;
                //if (age <= 0) age = 1; 
                CalculateNormalisedWeight(age);
            }
        }

        /// <summary>
        /// Date individual entered simulation 
        /// </summary>
        public DateTime DateEnteredSimulation { get; set; } = default;

        /// <summary>
        /// Date individual was purchased 
        /// </summary>
        public DateTime DateOfPurchase { get; set; } = default;

        /// <summary>
        /// The age (days) this individual entered the simulation.
        /// </summary>
        /// <units>Days</units>
        [FilterByProperty]
        public double AgeEnteredSimulation 
        {
            get
            {
                return DaysSince(RuminantTimeSpanTypes.Birth, double.NaN, DateEnteredSimulation);
            }
        }

        /// <summary>
        /// The age (days) of this individual at purchase.
        /// Inf value represents individuals that were not purchased.
        /// </summary>
        /// <units>Days</units>
        [FilterByProperty]
        public double AgeAtPurchase
        {
            get
            {
                if(DateOfPurchase == default) return double.NaN;
                return DaysSince(RuminantTimeSpanTypes.Birth, double.NaN, DateOfPurchase);
            }
        }

        ///// <summary>
        ///// Purchase age (Months)
        ///// </summary>
        ///// <units>Months</units>
        //[FilterByProperty]
        //public double PurchaseAge { get; set; }

        /// <summary>
        /// Number of months since purchased
        /// </summary>
        [FilterByProperty]
        public double DaysSincePurchase
        {
            get
            {
                if (DateOfPurchase == default) return double.NaN;
                return DaysSince(RuminantTimeSpanTypes.Purchased, double.NaN);
                //return Convert.ToInt32(Math.Round(AgeInDays - AgeAtPurchase, 4));
            }
        }

        #endregion

        #region Weight properties

        /// <summary>
        /// Calculate normalised weight from age of individual (in days)
        /// </summary>
        /// <param name="age">Age in days</param>
        /// <param name="forceNormMax">Use the max Norm</param>
        /// <param name="setForIndividual">Set individuals Normal weight after calculation</param>
        /// <returns>Normalised weight (kg)</returns>
        public double CalculateNormalisedWeight(int age, bool forceNormMax = false, bool setForIndividual = true)
        {
            // Original CLEM assumes 
            // * single births
            // * normalised weight always equals normalised max of new equations.
            // return StandardReferenceWeight - ((1 - Parameters.General.BirthScalar) * StandardReferenceWeight) * Math.Exp(-(Parameters.General.AgeGrowthRateCoefficient * age) / (Math.Pow(StandardReferenceWeight, Parameters.General.SRWGrowthScalar)));

            // ToDo: Check brackets in CLEM Equations.docx is the Exp applied only to the (1-BS)*SRW. I don't understand this equation.

            // ========================================================================================================================
            // Equation 1
            // Freer et al. (2012) The GRAZPLAN animal biology model for sheep and cattle and the GrazFeed decision support tool
            // CP15Y is determined at birth based on the number of siblings from the values provided in the params - Table 6 of SCA
            // ========================================================================================================================

            double normMax = Weight.StandardReferenceWeight - (Weight.StandardReferenceWeight - Weight.AtBirth) * Math.Exp(-(Parameters.General.AgeGrowthRateCoefficient_CN1 * age) / Math.Pow(Weight.StandardReferenceWeight, Parameters.General.SRWGrowthScalar_CN2));

            // ToDo: ensure this is appropriate for intervals greater than 1 day as cummulative effect should be considered.
            // ToDo: check that this needs to use Previous weight and not weight before modified

            // ========================================================================================================================
            // Equation 1a
            // Freer et al. (2012) The GRAZPLAN animal biology model for sheep and cattle and the GrazFeed decision support tool
            // ========================================================================================================================
            double normWeight = normMax;
            if (!forceNormMax && Weight.Base.Amount < normMax) // was weight previous but zero at start
                normWeight = Parameters.General.SlowGrowthFactor_CN3 * normMax + (1 - Parameters.General.SlowGrowthFactor_CN3) * Weight.Base.Amount;
            if (setForIndividual)
                Weight.SetNormalWeightForAge(normWeight, normMax);
            return normWeight;
        }

        /// <summary>
        /// Body condition score
        /// </summary>
        [FilterByProperty]
        public double BodyConditionScore
        {
            get
            {
                double bcscore = Parameters.General.BCScoreRange[1] + (Weight.RelativeCondition - 1) / Parameters.General.RelBCToScoreRate;
                return Math.Max(Parameters.General.BCScoreRange[0], Math.Min(bcscore, Parameters.General.BCScoreRange[2]));
            }
        }

        /// <summary>
        /// Current fleece weight as proportion of fleece weight expected for age
        /// </summary>
        public double ProportionFleeceAttained 
        {
            get
            {
                return Weight.FleeceWeightAsProportionOfSFW(Parameters, AgeInDays);
            }
        }

        /// <summary>
        /// Report protein required for maintenance pregnancy and lactation saved from reduced lactation (kg)
        /// </summary>
        public abstract double ProteinRequiredBeforeGrowth { get; }

        #endregion

        #region Classification properties

        /// <summary>
        /// A label combining sex and class for reporting
        /// </summary>
        [FilterByProperty]
        public string SexAndClass
        {
            get
            {
                return $"{Sex}.{Class}";
            }
        }

        /// <summary>
        /// Class for Breeder individuals
        /// </summary>
        public abstract string BreederClass { get; }

        /// <summary>
        /// Determine the category of this individual
        /// </summary>
        [FilterByProperty]
        public string Class
        {
            get
            {
                if (IsSuckling)
                    return "Suckling";
                else if (IsWeaner)
                    return "Weaner";

                if (Sex == Sex.Female)
                    return (this as RuminantFemale).BreederClass;
                return (this as RuminantMale).BreederClass;
            }
        }

        /// <summary>
        /// Determine the category of this individual with sex
        /// </summary>
        [FilterByProperty]
        public string FullCategory
        {
            get
            {
                return $"{Class}{Sex}";
            }
        }

        /// <summary>
        /// Get the value to use for the transaction style requested
        /// </summary>
        /// <param name="transactionStyle">Style of transaction grouping</param>
        /// <param name="pricingStyle">Style of pricing if necessary</param>
        /// <returns>Label to group by</returns>
        public string GetTransactionCategory(RuminantTransactionsGroupingStyle transactionStyle, PurchaseOrSalePricingStyleType pricingStyle = PurchaseOrSalePricingStyleType.Both)
        {
            string result = "N/A";
            switch (transactionStyle)
            {
                case RuminantTransactionsGroupingStyle.Combined:
                    return "All";
                case RuminantTransactionsGroupingStyle.ByPriceGroup:
                    return Parameters.Details.GetPriceGroupOfIndividual(this, pricingStyle)?.Name ?? $"{pricingStyle}NotSet";
                case RuminantTransactionsGroupingStyle.ByClass:
                    return this.Class;
                case RuminantTransactionsGroupingStyle.BySexAndClass:
                    return this.FullCategory;
                default:
                    break;
            }
            return result;
        }

        #endregion

        /// <summary>
        /// Return intake as a proportion of the potential intake.
        /// This includes milk for sucklings.
        /// </summary>
        [FilterByProperty]
        public double ProportionOfPotentialIntakeObtained
        {
            get
            {
                return Intake.ProportionOfPotentialIntakeObtained;
            }
        }

        /// <summary>
        /// Current monthly metabolic intake after crude protein adjustment (Grow v1)
        /// </summary>
        /// <units>kg/month</units>
        public double MetabolicIntake { get; set; }

        /// <summary>
        /// Flag to identify individual ready for sale
        /// </summary>
        [FilterByProperty]
        public HerdChangeReason SaleFlag { get; set; } = HerdChangeReason.None;

        /// <summary>
        /// Determines if the change reason is positive or negative
        /// </summary>
        public int PopulationChangeDirection
        {
            get
            {
                switch (SaleFlag)
                {
                    case HerdChangeReason.None:
                        return 0;
                    case HerdChangeReason.DiedUnderweight:
                    case HerdChangeReason.DiedMortality:
                    case HerdChangeReason.TradeSale:
                    case HerdChangeReason.DryBreederSale:
                    case HerdChangeReason.ExcessBreederSale:
                    case HerdChangeReason.ExcessSireSale:
                    case HerdChangeReason.MaxAgeSale:
                    case HerdChangeReason.AgeWeightSale:
                    case HerdChangeReason.ExcessPreBreederSale:
                    case HerdChangeReason.Consumed:
                    case HerdChangeReason.DestockSale:
                    case HerdChangeReason.ReduceInitialHerd:
                    case HerdChangeReason.MarkedSale:
                    case HerdChangeReason.WeanerSale:
                        return -1;
                    case HerdChangeReason.Born:
                    case HerdChangeReason.TradePurchase:
                    case HerdChangeReason.BreederPurchase:
                    case HerdChangeReason.SirePurchase:
                    case HerdChangeReason.RestockPurchase:
                    case HerdChangeReason.InitialHerd:
                    case HerdChangeReason.FillInitialHerd:
                        return 1;
                    default:
                        return 0;
                }
            }
        }

        /// <summary>
        /// Is the individual currently marked for sale?
        /// </summary>
        [FilterByProperty]
        public bool IsReadyForSale { get { return SaleFlag != HerdChangeReason.None; } }

        /// <summary>
        /// Method called on offspring when mother is lost (e.g. dies or sold)
        /// </summary>
        public void MotherLost()
        {
            if (Mother != null)
            {
                Mother.SucklingOffspringList.Remove(this);
                Mother = null;
            }
        }

        /// <summary>
        /// Wean this individual
        /// </summary>
        public void Wean(bool report, string reason, DateTime date)
        {
            dateOfWeaning = date;
            if (Mother != null)
            {
                Mother.SucklingOffspringList.Remove(this);
                Mother.NumberOfWeaned++;
            }
            if (report)
            {
                RuminantReportItemEventArgs args = new()
                {
                    RumObj = this,
                    Category = reason
                };
                (this.Parameters.Details.Parent as RuminantHerd).OnWeanOccurred(args);
            }
        }

        /// <summary>
        /// Age when individual must be weaned
        /// </summary>
        public int AgeToWeanNaturally
        {
            get
            {
                return MathUtilities.FloatsAreEqual(Parameters.General.NaturalWeaningAge.InDays, 0) ? Parameters.General.GestationLength.InDays : Parameters.General.NaturalWeaningAge.InDays;
            }
        }

        /// <summary>
        /// Milk production currently available for each offspring from mother (kg day-1)
        /// </summary>
        public double MothersMilkProductionAvailable
        {
            get
            {
                double milk = 0;
                if (this.Mother != null)
                {
                    // same location as mother and not isolated
                    if (this.Location == this.Mother.Location)
                    {
                        // distribute milk between offspring
                        int offspring = (this.Mother.SucklingOffspringList.Count <= 1) ? 1 : this.Mother.SucklingOffspringList.Count;
                        milk = this.Mother.Milk.ProductionRate / offspring;
                    }
                }
                return milk;
            }
        }

        /// <summary>
        /// Constructor based on Ruminant Cohort details
        /// </summary>
        /// <param name="date">The date of creation</param>
        /// <param name="setParams">The breed parameters for the individual</param>
        /// <param name="setAge">The age (days) of the individual</param>
        /// <param name="setWeight">The weight of the individual at creation</param>
        /// <param name="id">Unique id for individual (null if id not required e.g. purchases)</param>
        /// <param name="cohort">The cohort details for the individual</param>
        /// <param name="initialAttributes">The initial attributes specified for the individual</param>
        public Ruminant(DateTime date, RuminantParameters setParams, int setAge, double setWeight, int? id, RuminantTypeCohort cohort, IEnumerable<ISetAttribute> initialAttributes = null)
        {
            if (setParams is null)
                throw new Exception("Attempted to create a ruminant with no breed parameters provided");

            if (id is not null)
                ID = id ?? 0;

            Parameters = setParams;
            double birthScalar = Parameters.General?.BirthScalar[RuminantFemale.PredictNumberOfSiblingsFromBirthOfIndividual((Parameters.General?.MultipleBirthRate ?? null)) - 1] ?? 0.07;

            if ( birthScalar <= 0 || birthScalar > 1)
                throw new Exception($"Attempted to create a ruminant from [r={cohort.NameWithParent}] with invalid birthscalar [{birthScalar}].{Environment.NewLine}Expected 0 > birth scalar <= 1. Check BirthScalar property of [r={cohort.Parent.Parent.Name}] ");
            
            // create weight info object and set birth weight
            Weight = new(birthScalar * Parameters.General.SRWFemale);

            if (Sex == Sex.Female)
                Weight.SetStandardReferenceWeight(Parameters.General.SRWFemale);
            else
                Weight.SetStandardReferenceWeight(Parameters.General.SRWFemale * Parameters.General.SRWMaleMultiplier);

            Energy = new RuminantInfoEnergy(this);

            AgeInDays = setAge;
            DateOfBirth = date.AddDays(-1 * setAge);
            DateEnteredSimulation = date;

            int weanAge = AgeToWeanNaturally;
            if ((date - DateOfBirth).TotalDays > weanAge)
                dateOfWeaning = DateOfBirth.AddDays(weanAge);

            // if setweight is zero we need to set weight to normalised weight.
            if (setWeight <= 0)
                setWeight = CalculateNormalisedWeight(setAge, true);

            // Empty body weight to live weight assumes 1.09 conversion factor when no GrowPF parameters provided.
            Weight.AdjustByLiveWeightChange(setWeight, this);

            // determine fat and protein are required and adjust weight if needed
            cohort?.AssociatedHerd.RuminantGrowActivity?.SetInitialFatProtein(this, cohort, setWeight);

            // add fleece if required, may need weight to be set first so done here
            if (Parameters.General.IncludeWool && cohort.ProportionFleecePresent > 0)
            {
                Weight.WoolClean.Set(Weight.RelativeSize * Parameters.GrowPF_CW.StandardFleeceWeight * cohort.ProportionFleecePresent);
                Weight.Wool.Set(Weight.WoolClean.Amount / Parameters.GrowPF_CW.CleanToGreasyCRatio_CW3);
                Weight.UpdateLiveWeight();

                // TODO: this adds fleece onto the specified weight, rather than including the specified fleece weight in the supplied weight.
                // this should be called base weight (no fleece) in cohort.
            }

            Attributes = new IndividualAttributeList();

            if (cohort.Suckling)
            {
                if (AgeInDays >= ((setParams.General.NaturalWeaningAge.InDays == 0) ? setParams.General.GestationLength.InDays : setParams.General.NaturalWeaningAge.InDays))
                {
                    string limitstring = (setParams.General.NaturalWeaningAge.InDays == 0) ? $"gestation length [{setParams.General?.GestationLength ?? "Unknown"}]" : $"natural weaning age [{setParams.General.NaturalWeaningAge.InDays}]";
                    string warn = $"Individuals older than {limitstring} cannot be assigned as suckling [r={cohort.NameWithParent}]{Environment.NewLine}These individuals have not been assigned suckling.";
                    cohort.Warnings.CheckAndWrite(warn, cohort.Summary, cohort, MessageType.Warning);
                }
            }
            else
            {
                // the user has specified that this individual is not suckling, but it is younger than the weaning age, so wean today with no reporting.
                if (!IsWeaned)
                    Wean(false, string.Empty, date);
            }

            // initialise attributes
            foreach (ISetAttribute item in initialAttributes)
                this.AddNewAttribute(item);

        }

        /// <summary>
        /// Constructor based on details from mother for newborn
        /// </summary>
        /// <param name="date">The date of creation</param>
        /// <param name="id">Unique id for individual (null if id not required e.g. purchases)</param>
        /// <param name="mother">The mother of newborn</param>
        /// <param name="growActivity">Ruminant Grow Activity for fat and protein allocation if neeed</param>
        public Ruminant(DateTime date, int id, RuminantFemale mother, IRuminantActivityGrow growActivity)
        {
            double weight = mother.Weight.Fetus.Amount;
            double expectedWeight = mother.Parameters.General.BirthScalar[mother.NumberOfFetuses - 1] * mother.Weight.StandardReferenceWeight * (0.66 + (0.33 * mother.Weight.RelativeSize));
            if (mother.Weight.Fetus.Amount == 0)
                weight = expectedWeight;
            
            // previous calculation of expected weight, updated code will alter the birthweight calculation for CLEM.Grow
            // weight = Parameters.General.BirthScalar[NumberOfFetuses - 1] * Weight.StandardReferenceWeight * (1 - 0.33 * (1 - Weight.RelativeSizeByLiveWeight));

            ID = id;
            Parameters = new RuminantParameters(mother.Parameters);
            Location = mother.Location;
            Mother = mother;
            SaleFlag = HerdChangeReason.Born;

            // default weight as birth weight is assigned later.
            Weight = new();

            if (Sex == Sex.Female)
                Weight.SetStandardReferenceWeight(Parameters.General.SRWFemale);
            else
                Weight.SetStandardReferenceWeight(Parameters.General.SRWFemale * Parameters.General.SRWMaleMultiplier);

            Energy = new RuminantInfoEnergy(this);

            // pass to ruminant grow activity to determine how to set protein and fat at birth where the newborn has access to mother's properties
            growActivity?.SetProteinAndFatAtBirth(this);

            if (growActivity.IncludeFatAndProtein)
            {
                Weight.UpdateEBM(this);
            }
            else
            {
                // Empty body weight to live weight assumes 1.09 conversion factor when no GrowPF parameters provided.
                Weight.AdjustByLiveWeightChange(weight, this);
            }

            Weight.SetBirthWeightUsingCurrentWeight(this);

            // must get set after weight and birth weight are assignedf in order to calculate normalised weight correctly
            AgeInDays = 0;
            DateOfBirth = date;
            DateEnteredSimulation = date;

            // add attributes inherited from mother
            foreach (var attribute in mother.Attributes.Items.Where(a => a.Value is not null))
                AddInheritedAttribute(attribute);

            // create freemartin if needed (not allowed if no breeding params provided)
            if (Sex == Sex.Female && (Parameters.Breeding?.AllowFreemartins ?? false) && mother.MixedSexMultipleFetuses)
                AddNewAttribute(new SetAttributeWithValue() { AttributeName = "Freemartin", Category = RuminantAttributeCategoryTypes.Sterilise_Freemartin });

            // probability of dystocia 
            double dystociaRate = StdMath.SIG(Weight.Live / expectedWeight *
                                   Math.Max(Weight.RelativeCondition, 1.0), Parameters.Breeding.DystociaCoefficients);

            if (MathUtilities.IsLessThan(RandomNumberGenerator.Generator.NextDouble(), dystociaRate))
                AddNewAttribute(new SetAttributeWithValue() { AttributeName = "Dystocia", Category = RuminantAttributeCategoryTypes.Sterilise_Freemartin });
            //{
            //    Died = true;
            //    SaleFlag = HerdChangeReason.DiedDystocia;
            //}

            // add fleece expected from 1 day old individual if required
            if (Parameters.General.IncludeWool)
            {
                Weight.WoolClean.Set(Weight.FleeceWeightExpectedByAge(Parameters, 1));
                Weight.Wool.Set(Weight.WoolClean.Amount / Parameters.GrowPF_CW.CleanToGreasyCRatio_CW3);
            }

            Weight.UpdateLiveWeight();
        }

        /// <summary>
        /// Constructor to create an empty ruminant with a specified id for reported once dead
        /// </summary>
        /// <param name="id">Unique id for individual (null if id not required e.g. purchases)</param>
        public Ruminant(int id)
        {
            ID = id;
        }

        /// <summary>
        /// Factory for creating a Ruminant based on values provided from a ruminant cohort
        /// </summary>
        public static Ruminant Create(Sex sex, DateTime date, RuminantParameters parameters, int age, double weight = 0, int? id = null, RuminantTypeCohort cohortDetails = null, IEnumerable<ISetAttribute> initialAttributes = null, SetPreviousConception previousConception = null)
        {
            if (sex == Sex.Male)
                return new RuminantMale(date, parameters, age, weight, id, cohortDetails, initialAttributes);
            else
                return new RuminantFemale(date, parameters, age, weight, id, cohortDetails, initialAttributes, previousConception);
        }

        /// <summary>
        /// Factory for creating a new born Ruminant based on details obtained from mother
        /// </summary>
        public static Ruminant Create(Sex sex, DateTime date, int id, RuminantFemale mother, IRuminantActivityGrow growthActivity)
        {
            if (sex == Sex.Male)
                return new RuminantMale(date, id, mother, growthActivity);
            else
                return new RuminantFemale(date, id, mother, growthActivity);
        }


        /// <summary>
        /// Adds an attribute to an individual with ability to modify properties associated with the genotype
        /// </summary>
        /// <param name="attribute">base attribute from mother</param>
        public void AddInheritedAttribute(KeyValuePair<string, IIndividualAttribute> attribute)
        {
            // get inherited value
            IIndividualAttribute indAttribute = attribute.Value.GetInheritedAttribute() as IIndividualAttribute;

            // is this a property attribute that may modify the individuals parameter set?
            if(indAttribute?.SetAttributeSettings is SetAttributeWithProperty)
            {
                // has the value changed from that in the breed params provided to the individual?
                if (indAttribute.StoredValue != (attribute.Value.SetAttributeSettings as SetAttributeWithProperty).RuminantPropertyInfo.GetValue(this))
                {
                    // is this still the shared breed params with the mother
                    if(Parameters.Details != Mother.Parameters.Details)
                    {
                        // create deep copy of BreedParams


                    }
                    // update breedparams property to the new value
                    (attribute.Value.SetAttributeSettings as SetAttributeWithProperty).RuminantPropertyInfo.SetValue(this, indAttribute.StoredValue);
                }
            }
            Attributes.Add(attribute.Key, indAttribute);
        }

        /// <summary>
        /// Adds an attribute to an individual with ability to modify properties associated with the genotype
        /// </summary>
        /// <param name="attribute">base attribute from mother</param>
        public void AddNewAttribute(ISetAttribute attribute)
        {
            if (attribute is SetAttributeWithValue att)
                Sterilise(att.Category);

            // get inherited value
            IIndividualAttribute indAttribute = attribute.GetAttribute(true);

            // if it requires a property modifier then create new modified breedparams

            // if breedparams equals mother's create deep copy

            // update breedparams

            // save breed params to individual

            Attributes.Add(attribute.AttributeName, indAttribute); //.Value.GetInheritedAttribute() as IIndividualAttribute);
        }

        /// <summary>
        /// Add an attribute to this individual's list
        /// </summary>
        /// <param name="tag">Attribute label</param>
        /// <param name="category">Special category for the label</param>
        /// <param name="value">Value to set or change</param>
        public void AddNewAttribute(string tag, RuminantAttributeCategoryTypes category, IIndividualAttribute value = null)
        {
            Sterilise(category);
            Attributes.Add(tag, value);
        }

        /// <summary>
        /// Sterilise based on sex and category
        /// </summary>
        /// <param name="category"></param>
        private void Sterilise(RuminantAttributeCategoryTypes category)
        {
            switch (category)
            {
                case RuminantAttributeCategoryTypes.None:
                    break;
                case RuminantAttributeCategoryTypes.Sterilise_Castrate:
                    if (Sex == Sex.Male)
                        Sterilise(true);
                    break;
                case RuminantAttributeCategoryTypes.Sterilise_Freemartin:
                case RuminantAttributeCategoryTypes.Sterilise_Spay:
                case RuminantAttributeCategoryTypes.Sterilise_Webb:
                    if (Sex == Sex.Female)
                        Sterilise(false);
                    break;
                default:
                    break;
            }
        }
    }

    /// <summary>
    /// Sex of individuals
    /// </summary>
    public enum Sex
    {
        /// <summary>
        /// Female
        /// </summary>
        Female,
        /// <summary>
        /// Male
        /// </summary>
        Male
    };

}

