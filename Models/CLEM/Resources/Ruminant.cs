using APSIM.Shared.Utilities;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using System;
using System.Collections.Generic;
using System.Globalization;

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
        private double age;
        private double normalisedWeight;
        private double adultEquivalent;
        private int weaned = 0;

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

        /// <summary>
        /// Current animal price group for this individual 
        /// </summary>
        public (AnimalPriceGroup Buy, AnimalPriceGroup Sell) CurrentPriceGroups { get; set; } = (null, null);

        /// <inheritdoc/>
        public IndividualAttributeList Attributes { get; set; } = new IndividualAttributeList();

        /// <summary>
        /// Reference to the Breed Parameters.
        /// </summary>
        public RuminantType BreedParams;

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
        public bool IsSucklingWithMother { get { return weaned == 0 && mother != null; } }

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
        /// Age (Months)
        /// </summary>
        /// <units>Months</units>
        [FilterByProperty]
        public double Age
        {
            get
            {
                return age;
            }
            private set
            {
                age = value;
                normalisedWeight = CalculateNormalisedWeight(age);
            }
        }

        /// <summary>
        /// Age (Years)
        /// </summary>
        /// <units>Years</units>
        [FilterByProperty]
        public double AgeInYears
        {
            get
            {
                return age / 12.0;
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
                return Convert.ToInt32(Math.Floor(age / 12.0));
            }
        }

        /// <summary>
        /// Growth Rate (daily)
        /// </summary>
        /// <units>kg day-1</units>
        [FilterByProperty]
        public double GrowthRate
        {
            get
            {
                return (Weight - (BreedParams.SRWBirth * BreedParams.SRWFemale))/ Math.Max(1.0, age*30.4);
            }
        }

        /// <summary>
        /// Calculate normalised weight from age
        /// </summary>
        /// <param name="age">Age in months</param>
        /// <returns></returns>
        public double CalculateNormalisedWeight(double age)
        {
            return StandardReferenceWeight - ((1 - BreedParams.SRWBirth) * StandardReferenceWeight) * Math.Exp(-(BreedParams.AgeGrowthRateCoefficient * (age * 30.4)) / (Math.Pow(StandardReferenceWeight, BreedParams.SRWGrowthScalar)));
        }

        /// <summary>
        /// The age (months) this individual entered the simulation.
        /// </summary>
        /// <units>Months</units>
        [FilterByProperty]
        public double AgeEnteredSimulation { get; private set; }

        /// <summary>
        /// A method to set the age (months) this individual entered the simulation.
        /// This should be used with caution as this is usually a automatic calculation
        /// </summary>
        /// <units>Months</units>
        public void SetAgeEnteredSimulation(double age)
        {
            AgeEnteredSimulation = age;
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
        public int MonthsSincePurchase
        {
            get
            {
                return Convert.ToInt32(Math.Round(Age - PurchaseAge, 4));
            }
        }

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
                weight = value;

                adultEquivalent = Math.Pow(this.Weight, 0.75) / Math.Pow(this.BreedParams.BaseAnimalEquivalent, 0.75);

                // if highweight has not been defined set to initial weight
                if (HighWeight == 0)
                    HighWeight = weight;
                HighWeight = Math.Max(HighWeight, weight);
            }
        }

        /// <summary>
        /// Previous weight
        /// </summary>
        /// <units>kg</units>
        public double PreviousWeight { get; set; }

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
        /// The current weight as a proportion of Standard Reference Weight
        /// </summary>
        [FilterByProperty]
        public double ProportionOfSRW
        {
            get
            {
                return Weight / StandardReferenceWeight;
            }
        }

        /// <summary>
        /// The current health score -2 to 2 with 0 standard weight
        /// </summary>
        [FilterByProperty]
        public int HealthScore
        {
            get
            {
                throw new NotImplementedException("The Ruminant.HealthScore property is depeciated. Please use Body Condition Score.");
            }
        }

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
        /// Determine the category of this individual
        /// </summary>
        [FilterByProperty]
        public string Class
        {
            get
            {
                if (this.IsSuckling)
                    return "Suckling";
                else if (this.IsWeaner)
                    return "Weaner";
                else
                {
                    if (this is RuminantFemale female)
                    {
                        if (female.IsPreBreeder)
                            return "PreBreeder";
                        else if (female.IsBreeder)
                            return "Breeder";
                        else
                            return "Sterilized";
                    }
                    else
                    {
                        var male = this as RuminantMale;
                        if (male.IsSire)
                            return "Sire";
                        else if (male.IsCastrated)
                            return "Castrate";
                        else
                        {
                            if (male.IsWildBreeder)
                            {
                                return "Breeder";
                            }
                            else
                            {
                                return "PreBreeder";
                            }
                        }
                    }
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
        /// Is this individual a valid breeder and in condition
        /// </summary>
        [FilterByProperty]
        public virtual bool IsAbleToBreed { get { return false; } }

        /// <summary>
        /// Determine if weaned and less that 12 months old. Weaner
        /// </summary>
        [FilterByProperty]
        public bool IsWeaner
        {
            get
            {
                return (Weaned && Age < 12);
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
        /// Current monthly intake store
        /// </summary>
        /// <units>kg/month</units>
        public double Intake { get; set; }

        /// <summary>
        /// Current monthly intake of milk
        /// </summary>
        /// <units>kg/month</units>
        public double MilkIntake { get; set; }

        /// <summary>
        /// Required monthly intake of milk
        /// </summary>
        /// <units>kg/month</units>
        public double MilkPotentialIntake { get; set; }

        /// <summary>
        /// Percentage nitrogen of current intake
        /// </summary>
        public double PercentNOfIntake { get; set; }

        /// <summary>
        /// Diet dry matter digestibility of current monthly intake store
        /// </summary>
        /// <units>percent</units>
        public double DietDryMatterDigestibility { get; set; }

        /// <summary>
        /// Current monthly potential intake
        /// </summary>
        /// <units>kg/month</units>
        public double PotentialIntake { get; set; }

        /// <summary>
        /// Return intake as a proportion of the potential inake
        /// This includes milk for sucklings
        /// </summary>
        [FilterByProperty]
        public double ProportionOfPotentialIntakeObtained
        {
            get
            {
                if (MilkPotentialIntake > 0)
                {
                    double prop = (MilkIntake / MilkPotentialIntake);
                    if (MathUtilities.IsGreaterThanOrEqual(PotentialIntake, 0.0))
                    {
                        prop += (1 - (MilkIntake / MilkPotentialIntake)) * (Intake / PotentialIntake);
                    }
                    return prop;
                }
                else
                {
                    if (MathUtilities.IsGreaterThanOrEqual(PotentialIntake, 0.0))
                        return Intake / PotentialIntake;
                }
                return 0;
            }
        }

        /// <summary>
        /// Current monthly metabolic intake after crude protein adjustment
        /// </summary>
        /// <units>kg/month</units>
        public double MetabolicIntake { get; set; }

        /// <summary>
        /// Number in this class (1 if individual model)
        /// </summary>
        [FilterByProperty]
        public double Number { get; set; }

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
        /// Energy balance store
        /// </summary>
        public double EnergyBalance { get; set; }

        /// <summary>
        /// Energy used for milk production
        /// </summary>
        public double EnergyMilk { get; set; }

        /// <summary>
        /// Energy used for foetal development
        /// </summary>
        public double EnergyFetus { get; set; }

        /// <summary>
        /// Energy used for maintenance
        /// </summary>
        public double EnergyMaintenance { get; set; }

        /// <summary>
        /// Energy from intake
        /// </summary>
        public double EnergyIntake { get; set; }

        /// <summary>
        /// Indicates if this individual has died before removal from herd
        /// </summary>
        [FilterByProperty]
        public bool Died { get; set; }

        /// <summary>
        /// Standard Reference Weight determined from coefficients and gender
        /// </summary>
        /// <units>kg</units>
        [FilterByProperty]
        public double StandardReferenceWeight
        {
            get
            {
                if (Sex == Sex.Male)
                    return BreedParams.SRWFemale * BreedParams.SRWMaleMultiplier;
                else
                    return BreedParams.SRWFemale;
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
                double bcscore = BreedParams.BCScoreRange[1] + (RelativeCondition - 1) / BreedParams.RelBCToScoreRate;
                return Math.Max(BreedParams.BCScoreRange[0], Math.Min(bcscore, BreedParams.BCScoreRange[2]));
            }
        }

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
        public void Wean(bool report, string reason)
        {
            weaned = Convert.ToInt32(Math.Round(Age, 3), CultureInfo.InvariantCulture);
            if (weaned > Math.Ceiling(BreedParams.GestationLength))
                weaned = Convert.ToInt32(Math.Ceiling(BreedParams.GestationLength));

            if (Mother != null)
            {
                Mother.SucklingOffspringList.Remove(this);
                Mother.NumberOfWeaned++;
            }
            if (report)
            {
                RuminantReportItemEventArgs args = new RuminantReportItemEventArgs
                {
                    RumObj = this,
                    Category = reason
                };
                (this.BreedParams.Parent as RuminantHerd).OnWeanOccurred(args);
            }

        }

        /// <summary>
        /// Method to set the weaned status to unweaned for new born individuals.
        /// </summary>
        public void SetUnweaned()
        {
            weaned = 0;
        }

        /// <summary>
        /// Weaned individual flag
        /// </summary>
        [FilterByProperty]
        public bool Weaned { get { return weaned > 0; } }

        /// <summary>
        /// Weaned individual flag
        /// </summary>
        [FilterByProperty]
        public bool IsWeaned { get { return weaned > 0; } }

        /// <summary>
        /// Number of months since weaned
        /// </summary>
        [FilterByProperty]
        public int MonthsSinceWeaned
        {
            get
            {
                if (weaned > 0)
                    return Convert.ToInt32(Math.Round(Age - weaned, 4));
                else
                    return 0;
            }
        }

        /// <summary>
        /// Milk production currently available from mother
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
        /// A funtion to add intake and track changes in %N and DietDryMatterDigestibility
        /// </summary>
        /// <param name="intake">Feed request containing intake information kg, %N, DMD</param>
        public void AddIntake(FoodResourcePacket intake)
        {
            if (intake.Amount > 0)
            {
                // determine the adjusted DMD of all intake
                this.DietDryMatterDigestibility = ((this.Intake * this.DietDryMatterDigestibility) + (intake.DMD * intake.Amount)) / (this.Intake + intake.Amount);
                // determine the adjusted percentage N of all intake
                this.PercentNOfIntake = ((this.Intake * this.PercentNOfIntake) + (intake.PercentN * intake.Amount)) / (this.Intake + intake.Amount);
                this.Intake += intake.Amount;
            }
        }

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
        /// Method to increase age
        /// </summary>
        public void IncrementAge()
        {
            Age++;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Ruminant(RuminantType setParams, double setAge, double setWeight)
        {
            this.BreedParams = setParams;
            this.Age = setAge;
            this.AgeEnteredSimulation = setAge;

            Weight = setWeight <= 0 ? NormalisedAnimalWeight : setWeight;

            this.PreviousWeight = this.Weight;
            this.Number = 1;
            this.Wool = 0;
            this.Cashmere = 0;
            int ageInt = Convert.ToInt32(Math.Round(Age, 4));
            int weanage = Convert.ToInt32(Math.Round((BreedParams.NaturalWeaningAge == 0) ? BreedParams.GestationLength : BreedParams.NaturalWeaningAge, 4));//   Convert.ToInt32(Math.Round(BreedParams.GestationLength, 4));
            //this.weaned = (ageInt <= weanage)?ageInt:weanage;
            this.weaned = (ageInt < weanage) ? 0 : weanage;
            this.SaleFlag = HerdChangeReason.None;
            this.Attributes = new IndividualAttributeList();
        }

        /// <summary>
        /// Factory for creating ruminants based on provided values
        /// </summary>
        public static Ruminant Create(Sex sex, RuminantType parameters, double age = 0, double weight = 0)
        {
            if (sex == Sex.Male)
                return new RuminantMale(parameters, age, weight);
            else
                return new RuminantFemale(parameters, age, weight);
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

