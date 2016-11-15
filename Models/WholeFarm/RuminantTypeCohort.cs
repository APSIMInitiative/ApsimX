using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Xml.Serialization;
using Models.Core;

namespace Models.WholeFarm
{

	/// <summary>
	/// This stores the initialisation parameters for a Cohort of a specific Ruminant Type.
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantType))]
	public class RuminantTypeCohort : Model
	{

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
		/// Starting Price
		/// </summary>
		[Description("Starting Price")]
		public double StartingPrice { get; set; }

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

			parent = this.Parent as RuminantType;

			if (StartingNumber > 0)
			{
				//TODO: get random generator from global store with seed
				Random rand = new Random();
				for (int i = 1; i <= StartingNumber; i++)
				{
					Ruminant ruminant = new Ruminant();

					ruminant.BreedParams = parent;

					ruminant.Breed = parent.Breed;
					ruminant.Gender = Gender;
					ruminant.Age = StartingAge;
					ruminant.Weaned = !Suckling;

					double u1 = rand.NextDouble();
					double u2 = rand.NextDouble();
					double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
								 Math.Sin(2.0 * Math.PI * u2);
					ruminant.Weight = StartingWeight + StartingWeightSD * randStdNormal;
//					ruminant.Weight = StartingWeight;
					ruminant.Price = StartingPrice;

					ruminant.DryBreeder = true;
					ruminant.Number = 1;
					Individuals.Add(ruminant);
				}
			}

			return Individuals;
		}


	}
}



