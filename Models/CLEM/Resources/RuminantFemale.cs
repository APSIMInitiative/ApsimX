using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Object for an individual female Ruminant.
    /// </summary>

    public class RuminantFemale : Ruminant
    {
        // Female Ruminant properties

        /// <summary>
        /// The age of female at last birth
        /// </summary>
        public double AgeAtLastBirth { get; set; }

        /// <summary>
        /// Number of births for the female (twins = 1 birth)
        /// </summary>
        public int NumberOfBirths { get; set; }

        /// <summary>
        /// Births this timestep
        /// </summary>
        public int NumberOfBirthsThisTimestep { get; set; }

        /// <summary>
        /// The age at last conception
        /// </summary>
        public double AgeAtLastConception { get; set; }

        /// <summary>
        /// Weight at time of conception
        /// </summary>
        public double WeightAtConception { get; set; }

        /// <summary>
        /// Previous conception rate
        /// </summary>
        public double PreviousConceptionRate { get; set; }

        /// <summary>
        /// Weight lost at birth due to calf
        /// </summary>
        public double WeightLossDueToCalf { get; set; }

        /// <summary>
        /// Indicates if this female is a heifer
        /// Heifer equals less than min breed age and no offspring
        /// </summary>
        public bool IsHeifer
        {
            get
            {
                // wiki - weaned, no calf, <3 years. We use the ageAtFirstMating
                return (this.Weaned && this.NumberOfBirths == 0 && this.Age < this.BreedParams.MinimumAge1stMating);
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
                if(SuccessfulPregnancy)
                {
                    return this.Age >= this.AgeAtLastConception + this.BreedParams.GestationLength & this.AgeAtLastConception > this.AgeAtLastBirth;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Method to handle birth changes
        /// </summary>
        public void UpdateBirthDetails()
        {
            if (SuccessfulPregnancy)
            {
                NumberOfBirths++;
                NumberOfBirthsThisTimestep = (CarryingTwins ? 2 : 1);
            }
            AgeAtLastBirth = this.Age;
            MilkingPerformed = false;
        }

        /// <summary>
        /// Indicates if the individual is pregnant
        /// </summary>
        public bool IsPregnant
        {
            get
            {
                return (this.Age < this.AgeAtLastConception + this.BreedParams.GestationLength & this.SuccessfulPregnancy);
            }
        }

        /// <summary>
        /// Indicates if individual is carrying twins
        /// </summary>
        public bool CarryingTwins { get; set; }

        /// <summary>
        /// Method to remove one offspring that dies between conception and death
        /// </summary>
        public void OneOffspringDies()
        {
            if(CarryingTwins)
            {
                CarryingTwins = false;
            }
            else
            {
                SuccessfulPregnancy = false;
                AgeAtLastBirth = this.Age;

            }
        }

        /// <summary>
        /// Method to handle conception changes
        /// </summary>
        public void UpdateConceptionDetails(bool twins, double rate, int ageOffsett)
        {
            // if she was dry breeder remove flag as she has become pregnant.
            if (SaleFlag == HerdChangeReason.DryBreederSale)
            {
                SaleFlag = HerdChangeReason.None;
            }
            PreviousConceptionRate = rate;
            CarryingTwins = twins;
            WeightAtConception = this.Weight;
            AgeAtLastConception = this.Age + ageOffsett;
            SuccessfulPregnancy = true;
        }

        /// <summary>
        /// Indicates if the individual is a dry breeder
        /// </summary>
        public bool DryBreeder { get; set; }

        /// <summary>
        /// Indicates if the individual is lactating
        /// </summary>
        public bool IsLactating
        {
            get
            {
                // Had birth after last conception
                // Time since birth < milking days
                // Last pregnancy was successful
                // Mother has suckling offspring OR
                // Cow has been milked since weaning.
                return (this.AgeAtLastBirth > this.AgeAtLastConception & (this.Age - this.AgeAtLastBirth)*30.4 <= this.BreedParams.MilkingDays & SuccessfulPregnancy & (this.SucklingOffspring.Count() > 0 | this.MilkingPerformed));
            }            
        }

        /// <summary>
        /// Calculate the MilkinIndicates if the individual is lactating
        /// </summary>
        public double DaysLactating
        {
            get
            {
                if(IsLactating)
                {
                    double dl = (((this.Age - this.AgeAtLastBirth) * 30.4 <= this.BreedParams.MilkingDays) ? (this.Age - this.AgeAtLastBirth) * 30.4 : 0);
                    // add half a timestep
                    return dl+15;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Determines if milking has been performed on individual to increase milk production
        /// </summary>
        public bool MilkingPerformed { get; set; }

        /// <summary>
        /// Amount of milk available in the month (L)
        /// </summary>
        public double MilkAmount { get; set; }

        /// <summary>
        /// Potential amount of milk produced (L/day)
        /// </summary>
        public double MilkProductionPotential { get; set; }

        /// <summary>
        /// Amount of milk produced (L/day)
        /// </summary>
        public double MilkProduction { get; set; }

        /// <summary>
        /// Method to remove milk from female
        /// </summary>
        /// <param name="amount">Amount to take</param>
        public void TakeMilk(double amount)
        {
            amount = Math.Min(amount, MilkAmount);
            MilkAmount -= amount;
        }

        /// <summary>
        /// A list of individuals currently suckling this female
        /// </summary>
        public List<Ruminant> SucklingOffspring { get; set; }

        /// <summary>
        /// Used to track successful preganacy
        /// </summary>
        public bool SuccessfulPregnancy { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantFemale()
        {
            SuccessfulPregnancy = false;
            SucklingOffspring = new List<Ruminant>();
        }
    }
}
