using Models.CLEM.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Object for an individual Ruminant Animal.
    /// </summary>
    [Serializable]
    public class Ruminant
    {
        private RuminantFemale mother;
        private double weight;
        private double age;
        private double normalisedWeight;
        private double adultEquivalent;

        /// <summary>
        /// Reference to the Breed Parameters.
        /// </summary>
        public RuminantType BreedParams;

        /// <summary>
        /// Breed of individual
        /// </summary>
        public string Breed { get; set; }

        /// <summary>
        /// Herd individual belongs to
        /// </summary>
        public string HerdName { get; set; }

        /// <summary>
        /// Unique ID of individual
        /// </summary>
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
                {
                    MotherID = value.ID;
                }
            }
        }
        /// <summary>
        /// Link to individual's mother
        /// </summary>
        public int MotherID { get; private set; }

        /// <summary>
        /// Gender
        /// </summary>
        public Sex Gender { get; set; }

        /// <summary>
        /// Gender as string for reports
        /// </summary>
        public string GenderAsString { get { return Gender.ToString().Substring(0,1); } }

        /// <summary>
        /// Age (Months)
        /// </summary>
        /// <units>Months</units>
        public double Age
        {
            get
            {
                return age;
            }
            private set
            {
                age = value;
                normalisedWeight = StandardReferenceWeight - ((1 - BreedParams.SRWBirth) * StandardReferenceWeight) * Math.Exp(-(BreedParams.AgeGrowthRateCoefficient * (Age * 30.4)) / (Math.Pow(StandardReferenceWeight, BreedParams.SRWGrowthScalar)));
            }
        }

        /// <summary>
        /// The age (months) this individual entered the simulation.
        /// </summary>
        /// <units>Months</units>
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
        public double PurchaseAge { get; set; }

        /// <summary>
        /// Will return 0.1 if Age is 0 for calculations (Months)
        /// </summary>
        /// <units>Months</units>
        public double AgeZeroCorrected
        {  get { return ((Age == 0) ? 0.1 : Age); } }

        /// <summary>
        /// Weight (kg)
        /// </summary>
        /// <units>kg</units>
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
                {
                    HighWeight = weight;
                }
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
        public double WeightGain { get { return Weight - PreviousWeight; } }

        /// <summary>
        /// The adult equivalent of this individual
        /// </summary>
        public double AdultEquivalent { get { return adultEquivalent; } }
        // Needs to include ind.Number*weight if ever added to this model

        /// <summary>
        /// Highest previous weight
        /// </summary>
        /// <units>kg</units>
        public double HighWeight { get; private set; }

        /// <summary>
        /// The current weight as a proportion of High weight achieved
        /// </summary>
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
        public double ProportionOfNormalisedWeight
        {
            get
            {
                return NormalisedAnimalWeight == 0 ? 1 : Weight / NormalisedAnimalWeight;
            }
        }

        /// <summary>
        /// Is this individual a valid breeder and in condition
        /// </summary>
        public bool IsBreedingCondition 
        { 
            get
            {
                return (Gender == Sex.Male & Age >= BreedParams.MinimumAge1stMating) |
                    (Gender == Sex.Female &
                    (Age >= BreedParams.MinimumAge1stMating &
                    HighWeight >= BreedParams.MinimumSize1stMating * StandardReferenceWeight &
                    Age <= BreedParams.MaximumAgeMating)
                    );
            }
        }

        /// <summary>
        /// Determine if weaned and less that 12 months old. Weaner
        /// </summary>
        public bool Weaner
        {
            get
            {
                return (Weaned && Age<12);
            }
        }

        /// <summary>
        /// The current weight as a proportion of Standard Reference Weight
        /// </summary>
        public double ProportionOfSRW
        {
            get
            {
                return Weight / StandardReferenceWeight;
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
        public double MilkIntakePotential { get; set; }

        /// <summary>
        /// Percentage Nitrogen of current intake
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
        /// Current monthly metabolic intake after crude protein adjustment
        /// </summary>
        /// <units>kg/month</units>
        public double MetabolicIntake { get; set; }

        /// <summary>
        /// Number in this class (1 if individual model)
        /// </summary>
        public double Number { get; set; }

        /// <summary>
        /// Flag to identify individual ready for sale
        /// </summary>
        public HerdChangeReason SaleFlag { get; set; }

        /// <summary>
        /// List of individual tags
        /// </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Determines if the change resson is her positive or negative
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
                    case HerdChangeReason.ExcessHeiferSale:
                    case HerdChangeReason.Consumed:
                    case HerdChangeReason.DestockSale:
                    case HerdChangeReason.ReduceInitialHerd:
                    case HerdChangeReason.MarkedSale:
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
        /// SaleFlag as string for reports
        /// </summary>
        public string SaleFlagAsString { get { return SaleFlag.ToString(); } }

        /// <summary>
        /// Is the individual currently marked for sale?
        /// </summary>
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
        public double EnergyFoetus { get; set; }

        /// <summary>
        /// Energy used for maintenance
        /// </summary>
        public double EnergyMaintenance { get; set; }

        /// <summary>
        /// Energy from intake
        /// </summary>
        public double EnergyIntake { get; set; }

        /// <summary>
        /// Indicates if this individual has died
        /// </summary>
        public bool Died { get; set; }

        /// <summary>
        /// Standard Reference Weight determined from coefficients and gender
        /// </summary>
        /// <units>kg</units>
        public double StandardReferenceWeight
        {
            get
            {
                if (Gender == Sex.Male)
                {
                    return BreedParams.SRWFemale * BreedParams.SRWMaleMultiplier;
                }
                else
                {
                    return BreedParams.SRWFemale;
                }
            }
        }

        /// <summary>
        /// Normalised animal weight
        /// </summary>
        /// <units>kg</units>
        public double NormalisedAnimalWeight
        {
            get
            {
                return normalisedWeight;
                //return StandardReferenceWeight - ((1 - BreedParams.SRWBirth) * StandardReferenceWeight) * Math.Exp(-(BreedParams.AgeGrowthRateCoefficient * (Age * 30.4)) / (Math.Pow(StandardReferenceWeight, BreedParams.SRWGrowthScalar)));
            }
        }

        /// <summary>
        /// Relative size (normalised weight / standard reference weight)
        /// </summary>
        public double RelativeSize
        {
            get
            {
                return NormalisedAnimalWeight/StandardReferenceWeight;
            }
        }

        /// <summary>
        /// Relative condition (base weight / normalised weight)
        /// </summary>
        public double RelativeCondition
        {
            get
            {
                //TODO check that conceptus weight does not need to be removed for pregnant females.
                return Weight / NormalisedAnimalWeight;
            }
        }

        /// <summary>
        /// Wean this individual
        /// </summary>
        public void Wean(bool report, string reason)
        {
            weaned = true;
            if (this.Mother != null)
            {
                this.Mother.SucklingOffspringList.Remove(this);
                this.Mother.NumberOfWeaned++;
            }
            if(report)
            {
                RuminantReportItemEventArgs args = new RuminantReportItemEventArgs
                {
                    RumObj = this,
                    Reason = reason
                };
                (this.BreedParams.Parent as RuminantHerd).OnWeanOccurred(args);
            }

        }

        private bool weaned = true;

        /// <summary>
        /// Method to set the weaned status to unweaned for new born individuals.
        /// </summary>
        public void SetUnweaned()
        {
            weaned = false;
        }

        /// <summary>
        /// Weaned individual flag
        /// </summary>
        public bool Weaned { get { return weaned; } }

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
                this.PercentNOfIntake = ((this.Intake * this.PercentNOfIntake) + (intake.PercentN * intake.Amount)) / (this.Intake + intake.Amount); ;
                this.Intake += intake.Amount;
            }
        }

        /// <summary>
        /// Unique ID of the managed paddock the individual is located in.
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Amount of wool on individual
        /// </summary>
        public double Wool { get; set; }

        /// <summary>
        /// Amount of cashmere on individual
        /// </summary>
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
        public Ruminant(double setAge, Sex setGender, double setWeight, RuminantType setParams)
        {
            this.Gender = setGender;
            this.BreedParams = setParams;
            this.Age = setAge;
            this.AgeEnteredSimulation = setAge;

            if (setWeight <= 0)
            {
                // use normalised weight
                this.Weight = NormalisedAnimalWeight;
            }
            else
            {
                this.Weight = setWeight;
            }

            this.PreviousWeight = this.Weight;
            this.Number = 1;
            this.Wool = 0;
            this.Cashmere = 0;
            this.weaned = true;
            this.SaleFlag = HerdChangeReason.None;

            this.Tags = new List<string>();
        }
    }

    /// <summary>
    /// Sex of individuals
    /// </summary>
    public enum Sex
    {
        /// <summary>
        /// Male
        /// </summary>
        Male,
        /// <summary>
        /// Female
        /// </summary>
        Female
    };

}

