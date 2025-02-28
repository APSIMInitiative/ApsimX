using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.Core;
using Models.GrazPlan;
using Models.LifeCycle;
using PdfSharpCore.Pdf.Filters;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.Json.Serialization;

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
                if (!IsWeaned)
                    return "NotBreeder";
                if (!IsSterilised)
                {
                    if (IsPreBreeder)
                        return "PreBreeder";
                    return "Breeder";
                }
                if (IsSpayed)
                    return "Spayed";
                return "Webbed";
            }
        }

        /// <summary>
        /// Is the female webbed
        /// </summary>
        [FilterByProperty]
        public bool IsWebbed { get { return IsSterilised && Attributes.Exists("Webbed"); } }

        /// <summary>
        /// Is the female spayed
        /// </summary>
        [FilterByProperty]
        public bool IsSpayed { get { return IsSterilised && Attributes.Exists("Spayed"); } }

        /// <summary>
        /// Is female weaned and of minimum breeding age and weight and not sterilised 
        /// </summary>
        [FilterByProperty]
        public bool IsBreeder
        {
            get
            {
                return IsWeaned && !IsSterilised && !IsPreBreeder;
            }
        }

        /// <summary>
        /// Is this individual a valid breeder and in condition
        /// </summary>
        public override bool IsAbleToBreed
        {
            get
            {
                return (IsBreeder && !IsPregnant && DaysSince(RuminantTimeSpanTypes.GaveBirth, double.PositiveInfinity) >= Parameters.Breeding.MinimumDaysBirthToConception);
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
                // AL updated 28/10/2020. Removed ( && Age < MinimumAge1stMating ) as a heifer can be more than this age if first preganancy failed or missed.
                // this was a misunderstanding opn my part.
                return (IsWeaned && NumberOfBirths == 0);
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
                // AL updated 28/10/2020. Removed ( && Age < MinimumAge1stMating ) as a heifer can be more than this age if first preganancy failed or missed.
                // this was a misunderstanding opn my part.
                //return (Weaned && Age < MinimumAge1stMating); need to include size restriction as well
                return (IsWeaned && ((Weight.HighestAttained >= Parameters.General.MinimumSize1stMating * Weight.StandardReferenceWeight) & (AgeInDays >= Parameters.General.MinimumAge1stMating.InDays)) == false);
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

        /// <summary>
        /// Number of births for the female (twins = 1 birth)
        /// </summary>
        [FilterByProperty]
        public int NumberOfBirths { get; set; }

        /// <summary>
        /// Number of fetuses conceived in last conception
        /// </summary>
        [FilterByProperty]
        public int NumberOfFetuses { get; set; }

        /// <summary>
        /// Number of current sucklings
        /// </summary>
        [FilterByProperty]
        public int NumberOfSucklings { get { return SucklingOffspringList.Count; }  }

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

        /// <summary>
        /// Weight at time of conception
        /// </summary>
        public double WeightAtConception { get; set; }

        /// <summary>
        /// Live weight at parturition
        /// </summary>
        public double WeightAtParturition { get; set; }

        /// <summary>
        /// Highest weight achieved when not pregnant
        /// </summary>
        public double HighWeightWhenNotPregnant { get; private set; }

        /// <summary>
        /// Weight at 70% of pregnancy
        /// </summary>
        public double WeightAt70PctPregnant { get; set; }

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
                // 0.33 is CP4
                if(NumberOfFetuses > 0)
                    return (1 - 0.33 + 0.33 * Weight.RelativeSize) * CurrentBirthScalar * Weight.StandardReferenceWeight;
                return 0;
            }
        }

        /// <summary>
        /// Previous conception rate
        /// </summary>
        public double PreviousConceptionRate { get; set; }

        /// <summary>
        /// Store for the style of mating
        /// </summary>
        [FilterByProperty]
        public MatingStyle LastMatingStyle { get; set; } = MatingStyle.NotMated;

        /// <summary>
        /// Store for the style of mating
        /// </summary>
        [FilterByProperty]
        public ConceptionStatus LastConceptionStatus { get; set; } = ConceptionStatus.NotAvailable;

        /// <inheritdoc/>
        [FilterByProperty]
        public override string BreedingStatus
        {
            get
            {
                if (IsPregnant)
                    return "Pregnant";
                else if (IsLactating)
                    return "Lactating";
                else if (IsBreeder)
                {
                    if (IsAbleToBreed)
                    {
                        switch (LastConceptionStatus)
                        {
                            case ConceptionStatus.Failed:
                            case ConceptionStatus.Unsuccessful:
                            case ConceptionStatus.NotMated:
                            case ConceptionStatus.NotAvailable:
                                return LastConceptionStatus.ToString();
                            default:
                                break;
                        }
                    }
                    else
                    {
                        return "NotReady";
                    }
                }
                else
                    return "NotReady";
                return "NoBreeding";
            }
        }

        /// <summary>
        /// Calculate the number of offspring this preganacy given multiple offspring rates
        /// </summary>
        /// <returns></returns>
        public int CalulateNumberOfOffspringThisPregnancy()
        {
            int birthCount = 1;
            if (Parameters.General.MultipleBirthRate != null)
            {
                double rnd = RandomNumberGenerator.Generator.NextDouble();
                double birthProb = 0;
                foreach (double i in Parameters.General.MultipleBirthRate)
                {
                    birthCount++;
                    birthProb += i;
                    if (rnd <= birthProb)
                    {
                        NumberOfFetuses = birthCount;
                        return birthCount;
                    }
                }
                birthCount = 1;
            }
            NumberOfFetuses = birthCount;
            return birthCount;
        }

        /// <summary>
        /// Predict how many individuals were in the birth for a random individual
        /// </summary>
        /// <returns>Number of individuals from birth</returns>
        public static int PredictNumberOfSiblingsFromBirthOfIndividual(double[] mutibirthrates)
        {
            int[] number = new int[mutibirthrates.Length + 1];
            number[0] = Convert.ToInt32((1- mutibirthrates.Sum())*1000);
            for (int i = 0; i < mutibirthrates.Length; i++)
                number[i+1] = number[i] + Convert.ToInt32(1000 * mutibirthrates[i]*(i+2));

            int rnd = RandomNumberGenerator.Generator.Next(1, number.Last());

            int j = 0;
            while (j < number.Length && rnd > number[j])
                j++;
            return j + 1;
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
                    return DaysSince(RuminantTimeSpanTypes.Conceived, 0.0);
                else
                    return 0;
            }
        }

        /// <summary>
        /// Indicates if birth is due this time-step. 
        /// Knows whether the fetus(es) have survived
        /// </summary>
        public bool IsBirthDue
        {
            get
            {
                if (IsPregnant)
                    return MathUtilities.IsGreaterThan(DaysSince(RuminantTimeSpanTypes.Conceived, 0.0), Parameters.General.GestationLength.InDays);
                return false;
            }
        }

        /// <summary>
        /// Proportion of pregnancy achieved
        /// </summary>
        public double ProportionOfPregnancy(double offset = 0)
        {
            if (IsPregnant)
                return Math.Min(1.0, (DaysSince(RuminantTimeSpanTypes.Conceived, 0.0) + offset)/ Parameters.General.GestationLength.InDays);
            return 0;
        }

        /// <summary>
        /// Days since last birth
        /// </summary>
        public double? DaysSinceLastBirth
        {
            get
            {
                return DaysSince(RuminantTimeSpanTypes.GaveBirth, double.PositiveInfinity);
            }
        }

        /// <summary>
        /// Days since last birth
        /// </summary>
        public double DaysSinceLastConceived
        {
            get
            {
                return DaysSince(RuminantTimeSpanTypes.Conceived, double.NegativeInfinity);
            }
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
                return Fetuses.Any();
            }
        }

        /// <summary>
        /// Current birth scalar based on number of fetus carried
        /// </summary>
        [FilterByProperty]
        public double CurrentBirthScalar
        {
            get
            {
                if(NumberOfFetuses > 0)
                    return Parameters.General.BirthScalar[NumberOfFetuses-1];
                return 0;
            }
        }

        /// <summary>
        /// The sex of each fetus
        /// </summary>
        private List<Sex> Fetuses { get; set; } = new();

        /// <summary>
        /// Add fetus to female conception
        /// </summary>
        /// <param name="sex"></param>
        public void AddFetus(Sex sex)
        {
            Fetuses.Add(sex);
            MixedSexMultipleFetuses = Fetuses.Distinct().Count() > 1;
        }

        /// <summary>
        /// Are the fetuses of a multiple birth mixed sex
        /// </summary>
        public bool MixedSexMultipleFetuses { get; private set; }

        /// <summary>
        /// The number of fetuses being carryied
        /// </summary>
        public int CarryingCount { get { return Fetuses.Count; } }

        /// <summary>
        /// Method to remove one offspring that dies between conception and death
        /// </summary>
        public void OneFetusDies(DateTime date)
        {
            Fetuses.RemoveAt(0);
            if (!Fetuses.Any())
                DateOfLastBirth = date;
        }

        /// <summary>
        /// Was the last pregnancy successful
        /// </summary>
        public bool SuccessfulPregnancy
        {
            get
            {
                return TimeSince(RuminantTimeSpanTypes.Conceived, DateOfLastBirth).TotalDays == Parameters.General.GestationLength.InDays;
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

            for (int i = 0; i < number; i++)
            {
                bool isMale = RandomNumberGenerator.Generator.NextDouble() <= Parameters.Breeding.ProportionOffspringMale;
                Fetuses.Add(isMale ? Sex.Male : Sex.Female);
                WeightAt70PctPregnant = 0;
            }

            //ToDo: Check this is correct
            DateLastConceived = date.AddDays(ageOffset);
            // use normalised weight for age if offset provided for pre simulation allocation
            WeightAtConception = (ageOffset < 0) ? CalculateNormalisedWeight(Convert.ToInt32(TimeSince(RuminantTimeSpanTypes.Birth, DateLastConceived).TotalDays), true, false) : this.Weight.Base.Amount;
            NumberOfConceptions++;
            IsReplacementBreeder = false;
        }

        /// <summary>
        /// Determine any fetus mortality including new born
        /// </summary>
        /// <param name="events">A link to the CLEM event timer model</param>
        /// <param name="conceptionArgs">A link to standard conception args to use for reportinh</param>
        /// <returns>True if pregnacny is lost</returns>
        public bool FetusNewBornMortality(CLEMEvents events, ConceptionStatusChangedEventArgs conceptionArgs)
        {
            for (int i = 0; i < CarryingCount; i++)
            {
                if (MathUtilities.IsLessThan(RandomNumberGenerator.Generator.NextDouble(), Parameters.Breeding.PrenatalMortality / Parameters.General.GestationLength.InDays / ((events.TimeStep == TimeStepTypes.Monthly) ? 30.4 : events.Interval) + 1))  // ToDo: CLOCK adjust prenatal mortality to per time step..... divide timestep by interval..
                {
                    OneFetusDies(events.Clock.Today);
                    if (CarryingCount == 0)
                    {
                        // report conception status changed when last multiple birth dies.
                        conceptionArgs.Update(ConceptionStatus.Failed, this, events.Clock.Today);
                        Parameters.Details.OnConceptionStatusChanged(conceptionArgs);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Give birth to all fetuses
        /// </summary>
        /// <param name="herd">A link to the ruminant herd to place newborn</param>
        /// <param name="events">A link to the CLEM event timer model</param>
        /// <param name="conceptionArgs">A link to standard conception args to use for reporting</param>
        /// <param name="activity">A link to the activity model calling this method for reporting</param>
        public bool GiveBirth(RuminantHerd herd, CLEMEvents events, ConceptionStatusChangedEventArgs conceptionArgs, IModel activity)
        {
            if (!IsBirthDue)
                return false;
            
            for (int i = 0; i < CarryingCount; i++)
            {
                // Determine the best birth weight to use. This is now passed to each RuminantActivityGrow to decide how to set protein and fat at birth
                // RuminantGrow 
                //   * calculate birth weigth (Freer) Parameters.General.BirthScalar[NumberOfFetuses-1] * Weight.StandardReferenceWeight * (1 - 0.33 * (1 - Weight.Live / Weight.StandardReferenceWeight));
                // RuminantGrow24
                //   * use the weight of fetus at birth as calculated during pregnancy
                // RuminantGrowSCA
                // RuminantGrowOddy

                Ruminant newSuckling = Ruminant.Create(Fetuses[i], events.Clock.Today, herd.NextUniqueID, this, herd.RuminantGrowActivity);

                herd.AddRuminant(newSuckling, activity);

                // add to sucklings
                SucklingOffspringList.Add(newSuckling);

                // this now reports for each individual born not a birth event as individual wean events are reported
                conceptionArgs.Update(ConceptionStatus.Birth, this, events.Clock.Today);
                Parameters.Details.OnConceptionStatusChanged(conceptionArgs);
            }
            UpdateBirthDetails(events.Clock.Today);
            return true;
        }

        /// <summary>
        /// Method to handle birth changes
        /// </summary>
        public void UpdateBirthDetails(DateTime date)
        {
            if (Fetuses.Any())
            {
                NumberOfBirths++;
                NumberOfOffspring += CarryingCount;
                NumberOfBirthsThisTimestep = CarryingCount;
            }
            base.Weight.Conceptus.Reset();
            base.Weight.Fetus.Reset();
            base.Weight.ConceptusFat.Reset();
            base.Weight.ConceptusProtein.Reset();
            WeightAtParturition = Weight.Live;
            DateOfLastBirth = date;
            Milk.Lag = 1;
            Milk.PotentialRate = 0;
            Milk.ProductionRate = 0;
            Milk.ProductionRatePrevious = 0;
            Milk.MaximumRate = 0;
            Fetuses.Clear();
            Milk.MilkingPerformed = false;
            RelativeConditionAtParturition = Weight.RelativeCondition;
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
                return ((SucklingOffspringList.Any() | Milk.MilkingPerformed) && DaysSince(RuminantTimeSpanTypes.GaveBirth, double.PositiveInfinity) <= Parameters.Lactation.MilkingDays);
            }
        }

        /// <summary>
        /// Lactation information
        /// </summary>
        [JsonIgnore]
        [FilterByProperty]
        public RuminantInfoLactation Milk { get; set; } = new RuminantInfoLactation();

        /// <summary>
        /// Report protein required for maintenance pregnancy and lactation saved from reduced lactation (kg)
        /// </summary>
        public override double ProteinRequiredBeforeGrowth { get { return Weight.Protein.ForMaintenence + Weight.Protein.ForPregnancy + Milk.Protein + Weight.Protein.ForWool; } }

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
            if(SucklingOffspringList.Any() || Milk.MilkingPerformed)
            {
                // must be at least 1 to get milk production on day of birth. 
                double milkdays = Math.Max(0.0, TimeSince(RuminantTimeSpanTypes.GaveBirth).TotalDays) + halfIntervalOffset;
                if (milkdays <= Parameters.Lactation.MilkingDays)
                {
                    return milkdays;
                }
            }
            return 0;
        }

        /// <summary>
        /// A list of individuals currently suckling this female
        /// </summary>
        public List<Ruminant> SucklingOffspringList { get; set; }

        /// <summary>
        /// Constructor based on cohort details
        /// </summary>
        public RuminantFemale(DateTime date, RuminantParameters setParams, int setAge, double setWeight, int? id, RuminantTypeCohort cohortDetails, IEnumerable<ISetAttribute> initialAttributes = null, SetPreviousConception conception = null)
            : base(date, setParams, setAge, setWeight, id, cohortDetails, initialAttributes)
        {
            SucklingOffspringList = new List<Ruminant>();

            //ruminantFemale.WeightAtConception = ruminant.Weight.Live;
            //ruminantFemale.NumberOfBirths = 0;

            conception?.SetConceptionDetails(this);

            if (cohortDetails.Sire)
            {
                string warn = $"Breeding sire switch is not valid for individual females [r={cohortDetails.NameWithParent}]{Environment.NewLine}These individuals have not been assigned sires. Change Sex to Male to create sires in initial herd.";
                cohortDetails.Warnings.CheckAndWrite(warn, cohortDetails.Summary, cohortDetails, MessageType.Warning);
            }

            //ToDo: Set conceptus weight if pregnant. Do this where fetus are added
        }

        /// <summary>
        /// Constructor for new born female ruminant
        /// </summary>
        public RuminantFemale(DateTime date, int id, RuminantFemale mother, IRuminantActivityGrow growActivity)
            : base(date, id, mother, growActivity)
        {
            // needed for female specific actions
            SucklingOffspringList = new();
        }

        /// <summary>
        /// Constructor for blank female ruminant with specificed id.
        /// </summary>
        public RuminantFemale(int id)
            : base(id)
        {
        }


    }

}
