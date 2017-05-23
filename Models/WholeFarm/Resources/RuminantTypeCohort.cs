using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm.Resources
{
	/// <summary>
	/// Holder for all initial ruminant cohorts
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantType))]
	public class RuminantInitialCohorts : Model
	{
		/// <summary>
		/// Create the individual ruminant animals for this Ruminant Type (Breed)
		/// </summary>
		/// <returns></returns>
		public List<Ruminant> CreateIndividuals()
		{
			List<Ruminant> Individuals = new List<Ruminant>();

			List<RuminantTypeCohort> childNodes = this.Children.Where(a => a.GetType() == typeof(RuminantTypeCohort)).Cast<RuminantTypeCohort>().ToList();
			foreach (RuminantTypeCohort cohort in childNodes)
			{
				Individuals.AddRange(cohort.CreateIndividuals());
			}
			return Individuals;
		}
	}

	/// <summary>
	/// This stores the initialisation parameters for a Cohort of a specific Ruminant Type.
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantInitialCohorts))]
	public class RuminantTypeCohort : WFModel
	{
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Gender
		/// </summary>
		[Description("Gender")]
		public Sex Gender { get; set; }

		/// <summary>
		/// Starting Age (Months)
		/// </summary>
		[Description("Starting Age")]
		public int StartingAge { get; set; }

		/// <summary>
		/// Starting Number
		/// </summary>
		[Description("Starting Number")]
		public double StartingNumber { get; set; }

		/// <summary>
		/// Starting Weight
		/// </summary>
		[Description("Starting Weight (kg)")]
		public double StartingWeight { get; set; }

		/// <summary>
		/// Standard deviation of starting weight. Use 0 to use starting weight only
		/// </summary>
		[Description("Standard deviation of starting weight")]
		public double StartingWeightSD { get; set; }

		/// <summary>
		/// Is suckling?
		/// </summary>
		[Description("Still suckling?")]
		public bool Suckling { get; set; }

		/// <summary>
		/// Create the individual ruminant animals using the Cohort parameterisations.
		/// </summary>
		/// <returns></returns>
		public List<Ruminant> CreateIndividuals()
		{
			List<Ruminant> Individuals = new List<Ruminant>();

			IModel parentNode = Apsim.Parent(this, typeof(IModel));
			RuminantType parent = parentNode as RuminantType;

			// get Ruminant Herd resource for unique ids
			RuminantHerd ruminantHerd = Resources.RuminantHerd();

			parent = this.Parent as RuminantType;

			if (StartingNumber > 0)
			{
				for (int i = 1; i <= StartingNumber; i++)
				{
					object ruminantBase = null;
					if(this.Gender == Sex.Male)
					{
						ruminantBase = new RuminantMale();
					}
					else
					{
						ruminantBase = new RuminantFemale();
					}

					Ruminant ruminant = ruminantBase as Ruminant;

					ruminant.ID = ruminantHerd.NextUniqueID;
					ruminant.BreedParams = parent;
					ruminant.Breed = parent.Breed;
					ruminant.HerdName = parent.Name;
					ruminant.Gender = Gender;
					ruminant.Age = StartingAge;
					ruminant.SaleFlag = HerdChangeReason.None;
					if (Suckling) ruminant.SetUnweaned();

					double u1 = WholeFarm.RandomGenerator.NextDouble();
					double u2 = WholeFarm.RandomGenerator.NextDouble();
					double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
								 Math.Sin(2.0 * Math.PI * u2);
					ruminant.Weight = StartingWeight + StartingWeightSD * randStdNormal;
					ruminant.PreviousWeight = ruminant.Weight;

					if(this.Gender == Sex.Female)
					{
						RuminantFemale ruminantFemale = ruminantBase as RuminantFemale;
						ruminantFemale.DryBreeder = true;
						ruminantFemale.WeightAtConception = this.StartingWeight;
						ruminantFemale.NumberOfBirths = 0;
					}

					Individuals.Add(ruminantBase as Ruminant);
				}
			}

			return Individuals;
		}


	}
}



