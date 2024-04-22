﻿using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Reporting;
using Models.Core;
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
                return Weaned && !IsSterilised && !IsPreBreeder;
            }
        }

        /// <summary>
        /// Is this individual a valid breeder and in condition
        /// </summary>
        public override bool IsAbleToBreed
        {
            get
            {
                return (IsBreeder && !IsPregnant && TimeSince(RuminantTimeSpanTypes.GaveBirth).TotalDays >= Parameters.Breeding.MinimumDaysBirthToConception);
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
                return (Weaned && ((Weight.HighestAttained >= Parameters.General.MinimumSize1stMating * Weight.StandardReferenceWeight) & (AgeInDays >= Parameters.General.MinimumAge1stMating.InDays)) == false);
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
        /// Body condition at parturition
        /// </summary>
        public double BodyConditionParturition { get; set; }

        /// <summary>
        /// Live weight at parturition
        /// </summary>
        public double WeightAtParturition { get; set; }

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
                        return birthCount;
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
                    return TimeSince(RuminantTimeSpanTypes.Conceived).TotalDays >= Parameters.General.GestationLength.InDays;
                else
                    return false;
            }
        }

        /// <summary>
        /// Proportion of pregnancy achieved
        /// </summary>
        public double ProportionOfPregnancy(double offset = 0)
        {
            if (IsPregnant)
                return Math.Min(1.0, (TimeSince(RuminantTimeSpanTypes.Conceived).TotalDays + offset)/ Parameters.General.GestationLength.InDays);
            else
                return 0;
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
        public void OneOffspringDies(DateTime date)
        {
            Fetuses.RemoveAt(0);
            if (!Fetuses.Any())
                DateOfLastBirth = date;
        }

        /// <summary>
        /// Number of breeding months in simulation. Years since min breeding age or entering the simulation for breeding stats calculations..
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
            }

            //ToDo: Check this is correct
            DateLastConceived = date.AddDays(ageOffset);
            // use normalised weight for age if offset provided for pre simulation allocation
            WeightAtConception = (ageOffset < 0) ? CalculateNormalisedWeight(Convert.ToInt32(TimeSince(RuminantTimeSpanTypes.Birth, DateLastConceived).TotalDays), true, false) : this.Weight.Live;
            NumberOfConceptions++;
            ReplacementBreeder = false;
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
                    OneOffspringDies(events.Clock.Today);
                    if (CarryingCount == 0)
                    {
                        // report conception status changed when last multiple birth dies.
                        conceptionArgs.Update(ConceptionStatus.Failed, this, events.Clock.Today);
                        BreedDetails.OnConceptionStatusChanged(conceptionArgs);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Give birth to all fetuses
        /// </summary>
        /// <param name="herd">A link to the CLEM herd to place newborn</param>
        /// <param name="events">A link to the CLEM event timer model</param>
        /// <param name="conceptionArgs">A link to standard conception args to use for reportinh</param>
        /// <param name="activity">A link to the activity model calling this method for reporting</param>
        public bool GiveBirth(RuminantHerd herd, CLEMEvents events, ConceptionStatusChangedEventArgs conceptionArgs, IModel activity)
        {
            if (!BirthDue)
                return false;
            
            for (int i = 0; i < CarryingCount; i++)
            {
                //ToDo: Check this is correct for birthrate -- I think it should be -0.33 + (0.33 * xxxx)
                double weight = Parameters.General.BirthScalar[NumberOfFetuses] * Weight.StandardReferenceWeight * (1 - 0.33 * (1 - Weight.Live / Weight.StandardReferenceWeight));

                Ruminant newSucklingRuminant = Ruminant.Create(Fetuses[i], BreedDetails, events.TimeStepStart, 0, CurrentBirthScalar, weight);
                newSucklingRuminant.HerdName = HerdName;
                newSucklingRuminant.Breed = Parameters.General.Breed;
                newSucklingRuminant.ID = herd.NextUniqueID;
                newSucklingRuminant.Location = Location;
                newSucklingRuminant.Mother = this;
                newSucklingRuminant.Number = 1;
                // suckling/calf weight from Freer
                newSucklingRuminant.SaleFlag = HerdChangeReason.Born;

                // add attributes inherited from mother
                foreach (var attribute in Attributes.Items.Where(a => a.Value is not null))
                    newSucklingRuminant.AddInheritedAttribute(attribute);

                // create freemartin if needed
                if (newSucklingRuminant.Sex == Sex.Female && Parameters.Breeding.AllowFreemartins && MixedSexMultipleFetuses)
                    newSucklingRuminant.AddNewAttribute(new SetAttributeWithValue() { AttributeName = "Freemartin", Category = RuminantAttributeCategoryTypes.Sterilise_Freemartin });

                herd.AddRuminant(newSucklingRuminant, activity);

                // add to sucklings
                SucklingOffspringList.Add(newSucklingRuminant);

                // this now reports for each individual born not a birth event as individual wean events are reported
                conceptionArgs.Update(ConceptionStatus.Birth, this, events.Clock.Today);
                BreedDetails.OnConceptionStatusChanged(conceptionArgs);
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
            BodyConditionParturition = Weight.BodyCondition;
            WeightAtParturition = Weight.Live;
            DateOfLastBirth = date;
            ProportionMilkProductionAchieved = 1;
            MilkLag = 1;
            MilkProduction2 = 0;
            MilkProductionMax = 0;
            Fetuses.Clear();
            MilkingPerformed = false;
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
                return ((SucklingOffspringList.Any() | this.MilkingPerformed) && TimeSince(RuminantTimeSpanTypes.GaveBirth).TotalDays <= Parameters.General.MilkingDays);
            }
        }

        /// <summary>
        /// Lactation information
        /// </summary>
        [JsonIgnore]
        public RuminantInfoLactation Milk { get; set; } = new RuminantInfoLactation();

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
                if (milkdays <= Parameters.General.MilkingDays)
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
        /// Milk production 2 (MP2)
        /// </summary>
        public double MilkProduction2 { get; set; }

        /// <summary>
        /// Milk production max
        /// </summary>
        public double MilkProductionMax { get; set; }

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
                    throw new ApplicationException($"Unknown MilkUseReason [{reason}] in TakeMilk method of [r=RuminantFemale]");
            }
        }

        /// <summary>
        /// A list of individuals currently suckling this female
        /// </summary>
        public List<Ruminant> SucklingOffspringList { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantFemale(RuminantType setParams, DateTime date, int setAge, double birthScalar, double setWeight)
            : base(setParams, setAge, birthScalar, setWeight, date)
        {
            SucklingOffspringList = new List<Ruminant>();

            //ToDo: Set conceptus weight if needed
        }
    }

}
