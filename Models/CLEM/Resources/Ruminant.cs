using APSIM.Shared.Utilities;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Object for an individual Ruminant Animal.
    /// </summary>
    [Serializable]
    public abstract class Ruminant : IFilterable, IAttributable
    {
        private RuminantFemale mother;
        private double weight;
        private int age;
        private double normalisedWeight;
        private double adultEquivalent;
        private double proteinMass = 0;
        private double fatMass = 0;

        #region All new Grow SCA properties

        /// <summary>
        /// Ruminant intake manager
        /// </summary>
        [JsonIgnore]
        public RuminantIntake Intake = new();

        /// <summary>
        /// Store for tracking energy use
        /// </summary>
        [JsonIgnore]
        public RuminantEnergyInfo Energy { get; set; }

        /// <summary>
        /// Store for tracking ruminant outputs
        /// </summary>
        [JsonIgnore]
        public RuminantOutputInfo Output { get; set; } = new RuminantOutputInfo();

        /// <summary>
        /// Relative size based on highest weight achieved (High weight / standard reference weight)
        /// </summary>
        [FilterByProperty]
        public double RelativeSizeByHighWeight
        {
            // TODO: check this is right
            get { return HighWeight / StandardReferenceWeight; }
        }

        /// <summary>
        /// The protein mass of the individual
        /// </summary>
        public double ProteinMass { get { return proteinMass; } }

        /// <summary>
        /// Adjust the protein mass of the individual.
        /// </summary>
        /// <param name="amount">Amount to change by with sign.</param>
        public void AdjustProteinMass(double amount)
        {
            proteinMass += amount;
            proteinMass = Math.Max(0, proteinMass);
        }

        /// <summary>
        /// The fat mass of individual
        /// </summary>
        public double FatMass { get { return fatMass; } }

        /// <summary>
        /// Add fat mass to individual.
        /// </summary>
        /// <param name="amount">Amount to change by with sign.</param>
        public void AdjustFatMass(double amount)
        {
            fatMass += amount;
            fatMass = Math.Max(0, fatMass);
        }


        ///// <summary>
        ///// Energy used for wool production
        ///// </summary>
        //public double EnergyForWool { get; set; }

        ///// <summary>
        ///// Energy available after wool growth
        ///// </summary>
        //public double EnergyAfterWool { get { return EnergyFromIntake - EnergyForWool; } }

        ///// <summary>
        ///// Energy used for maintenance
        ///// </summary>
        //public double EnergyForMaintenance { get; set; }

        ///// <summary>
        ///// Energy used for fetal development
        ///// </summary>
        //public double EnergyForFetus { get; set; }

        ///// <summary>
        ///// Energy available after accounting for pregnancy
        ///// </summary>
        //public double EnergyAfterPregnancy { get { return EnergyAfterWool - EnergyForMaintenance - EnergyForFetus; } }

        ///// <summary>
        ///// Energy used for milk production
        ///// </summary>
        //public double EnergyForLactation { get; set; }

        ///// <summary>
        ///// Energy available after lactation demands
        ///// </summary>
        //public double EnergyAfterLactation { get { return EnergyAfterPregnancy - EnergyForLactation; } }

        ///// <summary>
        ///// Energy used for maintenance
        ///// </summary>
        //public double EnergyForGain { get; set; }

        ///// <summary>
        ///// Energy available for growth
        ///// </summary>
        //public double EnergyAvailableForGain { get; set; }

        /// <summary>
        /// Energy from intake (used in RuminantActivityGrow V1)
        /// </summary>
        public double EnergyFromIntake { get; set; }

        /// <summary>
        /// Digestible protein leaving the stomach
        /// </summary>
        public double DPLS { get; set; }

        #endregion

        /// <summary>
        /// Current animal price group for this individual 
        /// </summary>
        public (AnimalPriceGroup Buy, AnimalPriceGroup Sell) CurrentPriceGroups { get; set; } = (null, null);

        /// <inheritdoc/>
        public IndividualAttributeList Attributes { get; set; } = new IndividualAttributeList();

        #region General properties

        /// <summary>
        /// Reference to the RuminantType.
        /// </summary>
        public RuminantType BreedParams;

        /// <summary>
        /// Reference to the Breed Parameters.
        /// </summary>
        public RuminantParameters Parameters;

        /// <summary>
        /// Breed of individual
        /// </summary>
        [FilterByProperty]
        public string Breed { get; set; }

        /// <summary>
        /// Herd individual belongs to
        /// </summary>
        [FilterByProperty]
        public string HerdName { get; set; }

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
        public bool IsSucklingWithMother { get { return !Weaned && mother is not null; } }

        private DateTime dateOfWeaning = default;

        /// <summary>
        /// Weaned individual flag
        /// </summary>
        [FilterByProperty]
        public bool Weaned { get { return dateOfWeaning != default; } }

        /// <summary>
        /// Number of days since weaned
        /// </summary>
        [FilterByProperty]
        public int DaysSinceWeaned
        {
            get
            {
                if (Weaned)
                    return Convert.ToInt32(TimeSince(RuminantTimeSpanTypes.Weaned).TotalDays);
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
                return (Weaned && age <= (DateTime.IsLeapYear(DateOfBirth.Year)?366:365));
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
                return !Weaned;
            }
        }

        /// <summary>
        /// Number in this class (1 if individual model)
        /// </summary>
        public double Number { get; set; }

        /// <summary>
        /// Unique ID of the managed paddock the individual is located in.
        /// </summary>
        [FilterByProperty]
        public string Location { get; set; }

        /// <summary>
        /// Amount of wool on individual
        /// </summary>
        [FilterByProperty]
        public double Wool { get; set; }

        /// <summary>
        /// Amount of cashmere on individual
        /// </summary>
        [FilterByProperty]
        public double Cashmere { get; set; }

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
        /// Has the individual been sterilised (webbed, spayed or castrated)
        /// </summary>
        [FilterByProperty]
        public abstract bool IsSterilised { get; }

        /// <summary>
        /// Marked as a replacement breeder
        /// </summary>
        [FilterByProperty]
        public bool ReplacementBreeder { get; set; }

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
        /// The time-span since the birth of the individual
        /// </summary>
        /// <param name="spanType">The measure to provide</param>
        /// <param name="toDate">Date to calculate to or omit to use date known by individual</param>
        /// <returns>A TimeSpan representing the age of the individual</returns>
        public TimeSpan TimeSince(RuminantTimeSpanTypes spanType, DateTime toDate = default)
        {
            DateTime fromDate = default;
            switch(spanType)
            {
                case RuminantTimeSpanTypes.Birth:
                    fromDate = DateOfBirth;
                    break;
                case RuminantTimeSpanTypes.Weaned:
                    if(Weaned)
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
            if (toDate == default || fromDate == default  || toDate < fromDate)
                return TimeSpan.Zero;
            else
                return toDate - fromDate;
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
                //AgeInDays = value * 30.4;
                if (age <= 0) age = 1;                
                normalisedWeight = CalculateNormalisedWeight(age);
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
                return TimeSince(RuminantTimeSpanTypes.EnteredSimulation).TotalDays;
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
                if(DateOfPurchase == default) return double.PositiveInfinity;
                return TimeSince(RuminantTimeSpanTypes.Purchased).TotalDays;
            }
        }

        /// <summary>
        /// Purchase age (Months)
        /// </summary>
        /// <units>Months</units>
        [FilterByProperty]
        public double PurchaseAge { get; set; }

        /// <summary>
        /// Number of months since purchased
        /// </summary>
        [FilterByProperty]
        public int DaysSincePurchase
        {
            get
            {
                return Convert.ToInt32(Math.Round(AgeInDays - AgeAtPurchase, 4));
            }
        }

        #endregion

        #region Weight properties

        /// <summary>
        /// Weight (kg)
        /// </summary>
        /// <units>kg</units>
        [FilterByProperty]
        public double Weight
        {
            get
            {
                return weight;
            }
            set
            {
                PreviousWeight = weight;
                weight = value;

                adultEquivalent = Math.Pow(this.Weight, 0.75) / Math.Pow(this.Parameters.General.BaseAnimalEquivalent, 0.75);

                // if highweight has not been defined set to initial weight
                if (HighWeight == 0)
                    HighWeight = weight;
                HighWeight = Math.Max(HighWeight, weight);

                if(this is RuminantFemale female)
                    female.UpdateHighWeightWhenNotPregnant(weight);
            }
        }

        /// <summary>
        /// Standard Reference Weight determined from coefficients and gender
        /// </summary>
        /// <units>kg</units>
        [FilterByProperty]
        public double StandardReferenceWeight
        {
            get
            {
                if (Sex == Sex.Male && (this as RuminantMale).IsCastrated == false)
                    return Parameters.General.SRWFemale * Parameters.General.SRWMaleMultiplier;
                else
                    return Parameters.General.SRWFemale;
            }
        }

        /// <summary>
        /// Normalised animal weight
        /// </summary>
        /// <units>kg</units>
        [FilterByProperty]
        public double NormalisedAnimalWeight
        {
            get
            {
                return normalisedWeight;
            }
        }

        /// <summary>
        /// The birth scalar for this individual. Value from breed params birth scalars based on whether from a multiple birth
        /// </summary>
        public double BirthScalar { get; set; }

        /// <summary>
        /// Calculate normalised weight from age of individual (in days)
        /// </summary>
        /// <param name="age">Age in days</param>
        /// <returns>Normalised weight (kg)</returns>
        public double CalculateNormalisedWeight(int age)
        {
            // Original CLEM assumes 
            // * single births
            // * normalised weight always equals normalised max of new equations.
            // return StandardReferenceWeight - ((1 - BreedParams.BirthScalar) * StandardReferenceWeight) * Math.Exp(-(BreedParams.AgeGrowthRateCoefficient * age) / (Math.Pow(StandardReferenceWeight, BreedParams.SRWGrowthScalar)));

            // ToDo: Check brackets in CLEM Equations.docx is the Exp applied only to the (1-BS)*SRW. I don't understand this equation.
            double normMax = StandardReferenceWeight - ((1 - BirthScalar) * Parameters.General.SRWFemale) * Math.Exp(-(Parameters.General.AgeGrowthRateCoefficient_CN1 * age) / Math.Pow(StandardReferenceWeight, Parameters.General.SRWGrowthScalar_CN2));

            // CP15Y is determined at birth based on the number of siblings from the values provided in the params 
            // Table6 of SCA

            // ToDo: ensure this is appropriate for intervals greater than 1 day as cummulative effect should be considered.
            // ToDo: check that this needs to use Previous weight and not weight before modified
            if (PreviousWeight < normMax)
                return Parameters.General.SlowGrowthFactor_CN3 * normMax + (1 - Parameters.General.SlowGrowthFactor_CN3) * PreviousWeight;
            else
                return normMax;
        }

        /// <summary>
        /// Previous weight
        /// </summary>
        /// <units>kg</units>
        public double PreviousWeight { get; private set; }

        /// <summary>
        /// Weight gain
        /// </summary>
        /// <units>kg</units>
        [FilterByProperty]
        public double WeightGain { get { return Weight - PreviousWeight; } }

        /// <summary>
        /// The adult equivalent of this individual
        /// </summary>
        [FilterByProperty]
        public double AdultEquivalent { get { return adultEquivalent; } }
        // TODO: Needs to include ind.Number*weight if ever added to this model

        /// <summary>
        /// Highest previous weight
        /// </summary>
        /// <units>kg</units>
        [FilterByProperty]
        public double HighWeight { get; private set; }

        /// <summary>
        /// The current weight as a proportion of High weight achieved
        /// </summary>
        [FilterByProperty]
        public double ProportionOfHighWeight
        {
            get
            {
                return HighWeight == 0 ? 1 : Weight / HighWeight;
            }
        }

        /// <summary>
        /// The current weight as a proportion of High weight achieved
        /// </summary>
        [FilterByProperty]
        public double ProportionOfNormalisedWeight
        {
            get
            {
                return NormalisedAnimalWeight == 0 ? 1 : Weight / NormalisedAnimalWeight;
            }
        }

        /// <summary>
        /// Relative size (normalised weight / standard reference weight)
        /// </summary>
        [FilterByProperty]
        public double RelativeSize
        {
            get
            {
                return NormalisedAnimalWeight / StandardReferenceWeight;
            }
        }

        /// <summary>
        /// Relative condition (base weight / normalised weight)
        /// </summary>
        [FilterByProperty]
        public double RelativeCondition
        {
            get
            {
                //TODO check that conceptus weight does not need to be removed for pregnant females.
                return Weight / NormalisedAnimalWeight;
            }
        }

        /// <summary>
        /// Body condition score
        /// </summary>
        [FilterByProperty]
        public double BodyConditionScore
        {
            get
            {
                double bcscore = Parameters.General.BCScoreRange[1] + (RelativeCondition - 1) / Parameters.General.RelBCToScoreRate;
                return Math.Max(Parameters.General.BCScoreRange[0], Math.Min(bcscore, Parameters.General.BCScoreRange[2]));
            }
        }

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
                else
                {
                    return BreederClass;
                }
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
                    return BreedParams.GetPriceGroupOfIndividual(this, pricingStyle)?.Name ?? $"{pricingStyle}NotSet";
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
        public bool ReadyForSale { get { return SaleFlag != HerdChangeReason.None; } }

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
            //weaned = Convert.ToInt32(Math.Round(Age, 3), CultureInfo.InvariantCulture);
            //if (weaned > Math.Ceiling(BreedParams.GestationLength))
            //    weaned = Convert.ToInt32(Math.Ceiling(BreedParams.GestationLength));

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
                (this.BreedParams.Parent as RuminantHerd).OnWeanOccurred(args);
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
        /// Milk production currently available for each offspring from mother (L day-1)
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
                        milk = this.Mother.MilkProduction / offspring;
                    }
                }
                return milk;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="setParams">The breed parameters for the individual</param>
        /// <param name="setAge">The age (days) of the individual</param>
        /// <param name="birthScalar">The brith scalar for individual taking into account multiple births</param>
        /// <param name="setWeight">The weight of the individual at creation</param>
        /// <param name="date">The date of creation</param>
        public Ruminant(RuminantType setParams, int setAge, double birthScalar, double setWeight, DateTime date)
        {
            BreedParams = setParams;
            Parameters = setParams.Parameters;

            AgeInDays = setAge;
            DateOfBirth = date.AddDays(-1*setAge);
            DateEnteredSimulation = date;
            BirthScalar = birthScalar;

            Weight = (setWeight <= 0) ? NormalisedAnimalWeight : setWeight;
            PreviousWeight = Weight; 

            //ToDo: setup protein mass and fat mass for new individual

            Number = 1;
            Wool = 0;
            Cashmere = 0;

            int weanAge = AgeToWeanNaturally;
            if ((date - DateOfBirth).TotalDays > weanAge)
                dateOfWeaning = DateOfBirth.AddDays(weanAge);
            
            SaleFlag = HerdChangeReason.None;
            Attributes = new IndividualAttributeList();
            Energy = new RuminantEnergyInfo(this);
        }

        /// <summary>
        /// Factory for creating ruminants based on provided values
        /// </summary>
        public static Ruminant Create(Sex sex, RuminantType parameters, DateTime date, int age, double birthScalar, double weight = 0)
        {
            if (sex == Sex.Male)
                return new RuminantMale(parameters, date, age, birthScalar, weight);
            else
                return new RuminantFemale(parameters, date, age, birthScalar, weight);
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
                    if(BreedParams != Mother.BreedParams)
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
            // get inherited value
            IIndividualAttribute indAttribute = attribute.GetAttribute(true);

            // if it requires a property modifier then create new modified breedparams

            //if breedparams equals mother's create deep copy

            // update breedparams

            // save breed params to individual

            Attributes.Add(attribute.AttributeName, indAttribute); //.Value.GetInheritedAttribute() as IIndividualAttribute);
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

