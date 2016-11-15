using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>
	/// Object for an individual Ruminant Animal.
	/// </summary>
	public class Ruminant
	{
		/// <summary>
		/// Reference to the Breed Parameters.
		/// </summary>
		public RuminantType BreedParams;

		/// <summary>
		/// Breed of individual
		/// </summary>
		public string Breed { get; set; }

		/// <summary>
		/// Gender
		/// </summary>
		public Sex Gender { get; set; }

		/// <summary>
		/// Age (Months)
		/// </summary>
		/// <units>Months</units>
		public int Age { get; set; }

		/// <summary>
		/// Weight (kg)
		/// </summary>
		/// <units>kg</units>
		public double Weight { get; set; }

		/// <summary>
		/// Price
		/// </summary>
		/// <units>$ currency</units>
		public double Price { get; set; }

		/// <summary>
		/// Highest previous weight
		/// </summary>
		/// <units>kg</units>
		public double HighWeight { get; set; }

		/// <summary>
		/// Current monthly intake store
		/// </summary>
		/// <units>kg/month</units>
		public double Intake;

		/// <summary>
		/// Percentage Nitrogen of current intake
		/// </summary>
		public double PercentNOfIntake;

		/// <summary>
		/// Diet dry matter digestibility of current monthly intake store
		/// </summary>
		/// <units>percent</units>
		public double DietDryMatterDigestibility;
		
		/// <summary>
		/// Current monthly potential intake
		/// </summary>
		/// <units>kg/month</units>
		public double PotentialIntake;

		/// <summary>
		/// Normalised animal weight
		/// </summary>
		/// <units>kg</units>
		public double NormalisedAnimalWeight;

		/// <summary>
		/// Number in this class (1 if individual model)
		/// </summary>
		public double Number;

		/// <summary>
		/// Energy balance store
		/// </summary>
		public double EnergyBalance;

		/// <summary>
		/// Weaned individual flag
		/// </summary>
		public bool Weaned { get; set; }

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


		// FEMALE

		/// <summary>
		/// Indicates if the individual is a dry breeder
		/// </summary>
		public bool DryBreeder;

		/// <summary>
		/// Indicates if the individual is milking
		/// </summary>
		public bool Milk;

		/// <summary>
		/// The parity of the individual
		/// </summary>
		public double Parity;

		/// <summary>
		/// Number of calves
		/// </summary>
		public int NumberOfCalves;

		/// <summary>
		/// Is birth month
		/// </summary>
		public bool BirthMonth;

		/// <summary>
		/// Determines if individual is lactating
		/// </summary>
		public bool Lactating { get { return ((Gender==Sex.Female) & (!DryBreeder ^ Milk)); } }

		/// <summary>
		/// Weights for previous 12 months for calculating conception
		/// </summary>
		public double[] ConceptionWeights;

		/// <summary>
		/// Constructor
		/// </summary>
		public Ruminant()
		{
			if (Gender == Sex.Female)
			{
				ConceptionWeights = new double[12];
			}
		}

		/// <summary>
		/// A funtion to add intake and track changes in %N and DietDryMatterDigestibility
		/// </summary>
		/// <param name="intake">Feed request contianing intake information kg, %n, DMD</param>
		public void AddIntake(RuminantFeedRequest intake)
		{
			// determine the adjusted DMD of all intake
			this.DietDryMatterDigestibility = ((this.Intake * this.DietDryMatterDigestibility / 100.0) + (intake.FeedActivity.FeedType.DMD / 100.0 * intake.Amount)) / (this.Intake + intake.Amount) * 100.0;
			// determine the adjusted percentage N of all intake
			this.PercentNOfIntake = ((this.Intake * this.PercentNOfIntake / 100.0) + (intake.FeedActivity.FeedType.Nitrogen / 100.0 * intake.Amount)) / (this.Intake + intake.Amount) * 100.0; ;

			this.Intake += intake.Amount;
		}

		/// <summary>
		/// Unique ID of pasture the individual is located in.
		/// </summary>
		public Guid Location { get; set; }

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

