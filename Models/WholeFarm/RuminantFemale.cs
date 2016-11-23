using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
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
		public double AgeAtLastBirth;

		/// <summary>
		/// Number of births for the female (twins = 1 birth)
		/// </summary>
		public int NumberOfBirths;

		/// <summary>
		/// The age at last conception
		/// </summary>
		public double AgeAtLastConception;

		/// <summary>
		/// Weight at time of conception
		/// </summary>
		public double WeightAtConception;

		/// <summary>
		/// Previous conception rate
		/// </summary>
		public double PreviousConceptionRate;

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
					return this.Age >= this.AgeAtLastConception + this.BreedParams.GestationLength;
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
				AgeAtLastBirth = this.Age;
				SuccessfulPregnancy = false;
				NumberOfBirths++;
			}
		}

		/// <summary>
		/// Indicates if the individual is pregnant
		/// </summary>
		public bool IsPregnant
		{
			get
			{
				return (this.Age >= this.AgeAtLastConception + this.BreedParams.GestationLength & this.SuccessfulPregnancy);
			}
		}

		/// <summary>
		/// Indicates if individual is carrying twins
		/// </summary>
		public bool CarryingTwins;

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
			}
		}

		/// <summary>
		/// Method to handle conception changes
		/// </summary>
		public void UpdateConceptionDetails(bool Twins, double Rate)
		{
			// if she was dry breeder remove flag as she has become pregnant.
			if (SaleFlag == Common.HerdChangeReason.DryBreederSale)
			{
				SaleFlag = Common.HerdChangeReason.None;
			}
			PreviousConceptionRate = Rate;
			CarryingTwins = Twins;
			WeightAtConception = this.Weight;
			AgeAtLastConception = this.Age;
			SuccessfulPregnancy = true;
		}

		/// <summary>
		/// Indicates if the individual is a dry breeder
		/// </summary>
		public bool DryBreeder;

		/// <summary>
		/// Indicates if the individual is lactating
		/// </summary>
		public bool IsLactating
		{
			get
			{
				return (this.Age - this.AgeAtLastBirth <= this.BreedParams.MilkingDays & this.AgeAtLastBirth > 0);
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
					return ((this.Age - this.AgeAtLastBirth <= this.BreedParams.MilkingDays)? this.Age - this.AgeAtLastBirth : 0);
				}
				else
				{
					return 0;
				}
			}
		}

		/// <summary>
		/// Amount of milk available in the month
		/// </summary>
		public double MilkAmount;

		/// <summary>
		/// Amount of milk available in the month
		/// </summary>
		public double MilkProduction;

		/// <summary>
		/// A list of individuals currently suckling this female
		/// </summary>
		public List<Ruminant> SucklingOffspring;

		/// <summary>
		/// Used to track successful preganacy
		/// </summary>
		public bool SuccessfulPregnancy;

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
