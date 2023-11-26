using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Object for an individual female Ruminant.
    /// </summary>
    [Serializable]
    public class RuminantFemale : Ruminant
    {
        /// <inheritdoc/>
        public override Sex Sex { get { return Sex.Female; } }

        /// <inheritdoc/>
        public override string BreederClass
        {
            // is only used by class property
            get
            {
                if (IsPreBreeder)
                    return "PreBreeder";
                else
                {
                    if (IsSpayed)
                        return "Spayed";
                    else if (IsWebbed)
                        return "Webbed";
                    else
                        return "Breeder";
                }
            }
        }

        /// <inheritdoc/>
        [FilterByProperty]
        public override bool IsSterilised { get { return (IsWebbed || IsSpayed); } }

        /// <summary>
        /// Is the female webbed
        /// </summary>
        [FilterByProperty]
        public bool IsWebbed { get { return Attributes.Exists("Webbed"); } }

        /// <summary>
        /// Is the female spayed
        /// </summary>
        [FilterByProperty]
        public bool IsSpayed { get { return Attributes.Exists("Spayed"); } }

        /// <summary>
        /// Is female weaned and of minimum breeding age and weight and not sterilised 
        /// </summary>
        [FilterByProperty]
        public bool IsBreeder
        {
            get
            {
                return Weaned && !IsPreBreeder && !IsSterilised;
            }
        }

        /// <summary>
        /// Is this individual a valid breeder and in condition
        /// </summary>
        public override bool IsAbleToBreed
        {
            get
            {
                return (this.IsBreeder && !this.IsPregnant && TimeSince(RuminantTimeSpanTypes.GaveBirth).TotalDays >= BreedParams.MinimumDaysBirthToConception);
            }
        }

        /// <summary>
        /// Indicates if this female is a heifer
        /// Heifer equals less than min breed age and no offspring
        /// </summary>
        [FilterByProperty]
        public bool IsHeifer
        {
            get
            {
                // wiki - weaned, no calf, <3 years. We use the ageAtFirstMating
                // AL updated 28/10/2020. Removed ( && Age < BreedParams.MinimumAge1stMating ) as a heifer can be more than this age if first preganancy failed or missed.
                // this was a misunderstanding opn my part.
                return (Weaned && NumberOfBirths == 0);
            }
        }

        /// <summary>
        /// Indicates if this female is a weaned but less than age at first mating 
        /// </summary>
        [FilterByProperty]
        public bool IsPreBreeder
        {
            get
            {
                // wiki - weaned, no calf, <3 years. We use the ageAtFirstMating
                // AL updated 28/10/2020. Removed ( && Age < BreedParams.MinimumAge1stMating ) as a heifer can be more than this age if first preganancy failed or missed.
                // this was a misunderstanding opn my part.
                //return (Weaned && Age < BreedParams.MinimumAge1stMating); need to include size restriction as well
                return (Weaned && ((HighWeight >= BreedParams.MinimumSize1stMating * StandardReferenceWeight) & (AgeInDays >= BreedParams.MinimumAge1stMating.InDays)) == false);
            }
        }

        /// <summary>
        /// Date of last birth
        /// </summary>
        public DateTime DateOfLastBirth { get; set; }

        /// <summary>
        /// Date of last conception
        /// </summary>
        public DateTime DateLastConceived { get; set; }

        ///// <summary>
        ///// The age of female at last birth
        ///// </summary>
        //public double AgeAtLastBirth { get { return TimeSince(RuminantTimeSpanTypes.GaveBirth).TotalDays; } }

        ///// <summary>
        ///// The time (months) passed since last birth
        ///// Returns 0 for pre-first birth females
        ///// </summary>
        //[FilterByProperty]
        //public double MonthsSinceLastBirth
        //{
        //    get
        //    {
        //        if (AgeAtLastBirth > 0)
        //            return Age - AgeAtLastBirth;
        //        else
        //            return 0;
        //    }
        //}

        /// <summary>
        /// Number of births for the female (twins = 1 birth)
        /// </summary>
        [FilterByProperty]
        public int NumberOfBirths { get; set; }

        /// <summary>
        /// Number of offspring for the female
        /// </summary>
        [FilterByProperty]
        public int NumberOfOffspring { get; set; }

        /// <summary>
        /// Number of weaned offspring for the female
        /// </summary>
        [FilterByProperty]
        public int NumberOfWeaned { get; set; }

        /// <summary>
        /// Number of conceptions for the female
        /// </summary>
        [FilterByProperty]
        public int NumberOfConceptions { get; set; }

        /// <summary>
        /// Births this timestep
        /// </summary>
        public int NumberOfBirthsThisTimestep { get; set; }

        ///// <summary>
        ///// The age at last conception
        ///// </summary>
        //public double AgeAtLastConception { get { return TimeSince(RuminantTimeSpanTypes.Conceived).TotalDays; } }

        /// <summary>
        /// Weight at time of conception
        /// </summary>
        public double WeightAtConception { get; set; }

        /// <summary>
        /// Highest weight achieved when not pregnant
        /// </summary>
        public double HighWeightWhenNotPregnant { get; set; }

        /// <summary>
        /// Track the highest weight of a female when not pregnant
        /// </summary>
        /// <param name="weight"></param>
        public void UpdateHighWeightWhenNotPregnant(double weight)
        {
            if(!IsPregnant)
            {
                HighWeightWhenNotPregnant = Math.Max(HighWeightWhenNotPregnant, weight);
            }
        }

        /// <summary>
        /// Predicted birth weight of offspring scaled by mother's relative weight
        /// </summary>
        public double ScaledBirthWeight 
        { 
            get
            {
                return (1 - 0.33 + 0.33 * RelativeSize) * BreedParams.BirthScalar * StandardReferenceWeight;
            }
        }

        /// <summary>
        /// Previous conception rate
        /// </summary>
        public double PreviousConceptionRate { get; set; }

        ///// <summary>
        ///// Months since minimum breeding age or entering the population
        ///// </summary>
        //public double NumberOfBreedingMonths
        //{
        //    get
        //    {
        //        return Age - Math.Max(BreedParams.MinimumAge1stMating, AgeEnteredSimulation);
        //    }
        //}

        /// <summary>
        /// Store for the style of mating
        /// </summary>
        [FilterByProperty]
        public MatingStyle LastMatingStyle { get; set; } = MatingStyle.NotMated;

        /// <summary>
        /// Calculate the number of offspring this preganacy given multiple offspring rates
        /// </summary>
        /// <returns></returns>
        public int CalulateNumberOfOffspringThisPregnancy()
        {
            int birthCount = 1;
            if (BreedParams.MultipleBirthRate != null)
            {
                double rnd = RandomNumberGenerator.Generator.NextDouble();
                double birthProb = 0;
                foreach (double i in BreedParams.MultipleBirthRate)
                {
                    birthCount++;
                    birthProb += i;
                    if (rnd <= birthProb)
                        return birthCount;
                }
                birthCount = 1;
            }
            return birthCount;
        }

        /// <summary>
        /// Indicates if birth is due this month
        /// Knows whether the fetus(es) have survived
        /// </summary>
        public double DaysPregnant
        {
            get
            {
                if (IsPregnant)
                    return TimeSince(RuminantTimeSpanTypes.Conceived).TotalDays;
                else
                    return 0;
            }
        }

        /// <summary>
        /// Indicates if birth is due this month
        /// Knows whether the feotus(es) have survived
        /// </summary>
        public bool BirthDue
        {
            get
            {
                if (IsPregnant)
                    return TimeSince(RuminantTimeSpanTypes.Conceived).TotalDays >= BreedParams.GestationLength.InDays;
                else
                    return false;
            }
        }

        /// <summary>
        /// Proportion of pregnancy achieved
        /// </summary>
        public double ProportionOfPregnancy
        {
            get
            {
                if (IsPregnant)
                    return TimeSince(RuminantTimeSpanTypes.Conceived).TotalDays/BreedParams.GestationLength.InDays;
                else
                    return 0;
            }
        }


        /// <summary>
        /// Method to handle birth changes
        /// </summary>
        public void UpdateBirthDetails(DateTime date)
        {
            if (CarryingCount > 0)
            {
                NumberOfBirths++;
                NumberOfOffspring += CarryingCount;
                NumberOfBirthsThisTimestep = CarryingCount;
            }
            DateOfLastBirth = date;
            ProportionMilkProductionAchieved = 1;
            MilkLag = 1;
            CarryingCount = 0;
            MilkingPerformed = false;
            RelativeConditionAtParturition = RelativeCondition;
        }

        /// <summary>
        /// Allows an activity to pre calculate conception rate
        /// </summary>
        public double? ActivityDeterminedConceptionRate { get; set; }

        /// <summary>
        /// Indicates if the individual is pregnant
        /// </summary>
        [FilterByProperty]
        public bool IsPregnant
        {
            get
            {
                return (CarryingCount > 0);
            }
        }

        /// <summary>
        /// Indicates if individual is carrying multiple fetuses
        /// </summary>
        public int CarryingCount { get; set; }

        /// <summary>
        /// Method to remove one offspring that dies between conception and death
        /// </summary>
        public void OneOffspringDies(DateTime date)
        {
            CarryingCount--;
            if (CarryingCount <= 0)
                DateOfLastBirth = date;
        }

        /// <summary>
        /// Number of breeding months in simulation. Years since min breeding age or entering the simulation for breeding stats calculations..
        /// </summary>
        public bool SuccessfulPregnancy
        {
            get
            {
                return TimeSince(RuminantTimeSpanTypes.Conceived, DateOfLastBirth).TotalDays == this.BreedParams.GestationLength.InDays;
            }
        }

        /// <summary>
        /// Method to handle conception changes
        /// </summary>
        public void UpdateConceptionDetails(int number, double rate, int ageOffset, DateTime date)
        {
            // if she was dry breeder remove flag as she has become pregnant.

            if (SaleFlag == HerdChangeReason.DryBreederSale)
                SaleFlag = HerdChangeReason.None;

            PreviousConceptionRate = rate;
            CarryingCount = number;

            //ToDo: Chech this is correct
            DateLastConceived = date.AddDays(ageOffset);
            //AgeAtLastConception = this.Age + ageOffset;
            // use normalised weight for age if offset provided for pre simulation allocation
            WeightAtConception = (ageOffset < 0) ? this.CalculateNormalisedWeight(Convert.ToInt32(TimeSince(RuminantTimeSpanTypes.Birth, DateLastConceived).TotalDays)) : this.Weight;
            NumberOfConceptions++;
            ReplacementBreeder = false;
        }

        /// <summary>
        /// Indicates if the individual is lactating
        /// </summary>
        [FilterByProperty]
        public bool IsLactating
        {
            get
            {
                //(a)Has at least one suckling offspring(i.e.unweaned offspring)
                //Or
                //(b) Is being milked
                //and
                //(c) Less than Milking days since last birth
                return ((this.SucklingOffspringList.Any() | this.MilkingPerformed) && TimeSince(RuminantTimeSpanTypes.GaveBirth).TotalDays <= this.BreedParams.MilkingDays);
            }
        }

        /// <summary>
        /// Lactation information
        /// </summary>
        public RuminantLactationInfo Milk { get; set; } = new RuminantLactationInfo();

        /// <summary>
        /// The proportion of the potential milk production achieved in timestep
        /// </summary>
        public double ProportionMilkProductionAchieved { get; set; }

        /// <summary>
        /// The body condition score at birth.
        /// </summary>
        public double RelativeConditionAtParturition { get; set; }

        /// <summary>
        /// Calculate the the number of days lacating for the individual
        /// </summary>
        /// <param name="halfIntervalOffset">Number of days to offset (e.g. half time step)</param>
        public double DaysLactating(double halfIntervalOffset = 0)
        {
            if(SucklingOffspringList.Any() || MilkingPerformed)
            {
                double milkdays = TimeSince(RuminantTimeSpanTypes.GaveBirth).TotalDays + halfIntervalOffset;
                if (milkdays <= BreedParams.MilkingDays)
                {
                    return milkdays;
                }
            }
            return 0;
        }

        /// <summary>
        /// Lag term for milk production
        /// </summary>
        public double MilkLag { get; set; }

        /// <summary>
        /// Tracks the nutrition after peak lactation for milk production.
        /// </summary>
        public double NutritionAfterPeakLactationFactor { get; set; }

        /// <summary>
        /// Determines if milking has been performed on individual to increase milk production
        /// </summary>
        [FilterByProperty]
        public bool MilkingPerformed { get; set; }

        /// <summary>
        /// Amount of milk available in the month (L)
        /// </summary>
        public double MilkCurrentlyAvailable { get; set; }

        /// <summary>
        /// Potential amount of milk produced (L/day)
        /// </summary>
        public double MilkProductionPotential { get; set; }

        /// <summary>
        /// Amount of milk produced (L/day)
        /// </summary>
        public double MilkProduction { get; set; }

        /// <summary>
        /// Amount of milk produced this time step
        /// </summary>
        public double MilkProducedThisTimeStep { get; set; }

        /// <summary>
        /// Amount of milk suckled this time step
        /// </summary>
        public double MilkSuckledThisTimeStep { get; set; }

        /// <summary>
        /// Amount of milk milked this time step
        /// </summary>
        public double MilkMilkedThisTimeStep { get; set; }

        /// <summary>
        /// Method to remove milk from female
        /// </summary>
        /// <param name="amount">Amount to take</param>
        /// <param name="reason">Reason for taking milk</param>
        public void TakeMilk(double amount, MilkUseReason reason)
        {
            amount = Math.Min(amount, MilkCurrentlyAvailable);
            MilkCurrentlyAvailable -= amount;
            switch (reason)
            {
                case MilkUseReason.Suckling:
                    MilkSuckledThisTimeStep += amount;
                    break;
                case MilkUseReason.Milked:
                    MilkMilkedThisTimeStep += amount;
                    break;
                default:
                    throw new ApplicationException("Unknown MilkUseReason [" + reason + "] in TakeMilk method of [r=RuminantFemale]");
            }
        }

        /// <summary>
        /// A list of individuals currently suckling this female
        /// </summary>
        public List<Ruminant> SucklingOffspringList { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantFemale(RuminantType setParams, DateTime date, int setAge, double setWeight)
            : base(setParams, setAge, setWeight, date)
        {
            SucklingOffspringList = new List<Ruminant>();
        }
    }

    /// <summary>
    /// Reasons for milk to be taken from female
    /// </summary>
    public enum MilkUseReason
    {
        /// <summary>
        /// Consumed by sucklings
        /// </summary>
        Suckling,
        /// <summary>
        /// Milked
        /// </summary>
        Milked
    }

    /// <summary>
    /// Style of mating
    /// </summary>
    public enum MatingStyle
    {
        /// <summary>
        /// Natural mating
        /// </summary>
        Natural,
        /// <summary>
        /// Controlled mating
        /// </summary>
        Controlled,
        /// <summary>
        /// Wild breeder
        /// </summary>
        WildBreeder,
        /// <summary>
        /// Mating assigned at setup
        /// </summary>
        PreSimulation,
        /// <summary>
        /// Individual not mated
        /// </summary>
        NotMated
    }
}
