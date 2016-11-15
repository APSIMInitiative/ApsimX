using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	/// <summary>
	/// Object for an individual Ruminant Animal.
	/// </summary>
	[Serializable]
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
		public RuminantFemale Mother { get; set; }

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
		public int Age { get; set; }

		/// <summary>
		/// Weight (kg)
		/// </summary>
		/// <units>kg</units>
		public double Weight { get; set; }

		/// <summary>
		/// Previous weight (kg)
		/// </summary>
		/// <units>kg</units>
		public double PreviousWeight { get; set; }

		/// <summary>
		/// Previous weight (kg)
		/// </summary>
		/// <units>kg</units>
		public double WeightGain { get { return Weight - PreviousWeight; } }

		/// <summary>
		/// The adult equivalent of this individual
		/// </summary>
		public double AdultEquivalent { get { return Math.Pow(this.Weight, 0.75) / Math.Pow(this.BreedParams.BaseAnimalEquivalent, 0.75); } }
//		public double AdultEquivalent { get { return this.Number * Math.Pow(this.Weight, 0.75) / Math.Pow(this.BreedParams.BaseAnimalEquivalent, 0.75); } }

		/// <summary>
		/// Highest previous weight
		/// </summary>
		/// <units>kg</units>
		public double HighWeight { get; set; }

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
		/// Normalised animal weight
		/// </summary>
		/// <units>kg</units>
		public double NormalisedAnimalWeight { get; set; }

		/// <summary>
		/// Number in this class (1 if individual model)
		/// </summary>
		public double Number { get; set; }

		/// <summary>
		/// Flag to identify individual ready for sale
		/// </summary>
		public Common.HerdChangeReason  SaleFlag { get; set; }

		/// <summary>
		/// SaleFlag as string for reports
		/// </summary>
		public string SaleFlagAsString { get { return SaleFlag.ToString(); } }

		/// <summary>
		/// Is the individual currently marked for sale?
		/// </summary>
		public bool ReadyForSale { get { return SaleFlag != Common.HerdChangeReason.None; } }

		/// <summary>
		/// Energy balance store
		/// </summary>
		public double EnergyBalance { get; set; }

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
		/// Wean this individual
		/// </summary>
		public void Wean()
		{
			weaned = true;
			if (this.Mother != null)
			{
				this.Mother.SucklingOffspring.Remove(this);
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
		public double MothersMilkAvailable { get
			{
				double milk = 0;
				if (this.Mother != null)
				{
					// same location as mother and not isolated
					if (this.Location == this.Mother.Location)
					{
						if (this.Mother.CarryingTwins)
						{
							// distribute milk between offspring
							milk = this.Mother.MilkProduction / 2;
						}
						else
						{
							milk = this.Mother.MilkProduction;
						}
					}
					this.Mother.MilkAmount -= milk;
				}
				return milk;
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
		/// Unique ID of the managed paddock the individual is located in.
		/// </summary>
		public string Location { get; set; }

		/// <summary>
		/// Amount of wool on individual
		/// </summary>
		public double Wool { get; set; }

		/// <summary>
		/// Amount of wool on individual
		/// </summary>
		public double Cashmere { get; set; }


		/// <summary>
		/// Constructor
		/// </summary>
		public Ruminant()
		{
			this.Number = 1;
			this.Wool = 0;
			this.Cashmere = 0;
			this.weaned = true;
			this.SaleFlag = Common.HerdChangeReason.None;
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

