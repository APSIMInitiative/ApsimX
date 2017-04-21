using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>Other animals breed activity</summary>
	/// <summary>This activity handles breeding in other animals types</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class OtherAnimalsActivityBreed : WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;
		[XmlIgnore]
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// Get the Clock.
		/// </summary>
		[Link]
		Clock Clock = null;

		/// <summary>
		/// name of other animal type
		/// </summary>
		[Description("Name of other animal type")]
		public string AnimalType { get; set; }

		/// <summary>
		/// Offspring per female breeder
		/// </summary>
		[Description("Offspring per female breeder")]
		public double OffspringPerBreeder { get; set; }

		/// <summary>
		/// Start breeding month
		/// </summary>
		[Description("First month of breeding")]
		public int StartBreedingMonth { get; set; }

		/// <summary>
		/// Breeding interval (months)
		/// </summary>
		[Description("Breeding interval (months)")]
		public int BreedingInterval { get; set; }

		/// <summary>
		/// Cost per female breeder
		/// </summary>
		[Description("Cost per female breeder")]
		public int CostPerBreeder { get; set; }

		/// <summary>
		/// Breeding female age
		/// </summary>
		[Description("Breeding age (months)")]
		public int BreedingAge { get; set; }

		/// <summary>
		/// Use local males for breeding
		/// </summary>
		[Description("Use local males for breeding")]
		public bool UseLocalMales { get; set; }

		/// <summary>
		/// The Other animal type this group points to
		/// </summary>
		public OtherAnimalsType SelectedOtherAnimalsType;

		/// <summary>
		/// Month this overhead is next due.
		/// </summary>
		[XmlIgnore]
		public DateTime NextDueDate { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			SelectedOtherAnimalsType = Resources.OtherAnimalsStore().GetByName(AnimalType) as OtherAnimalsType;
			if (SelectedOtherAnimalsType == null)
			{
				throw new Exception("Unknown other animal type: " + AnimalType + " in OtherAnimalsActivityFeed : " + this.Name);
			}

			if (BreedingInterval <= 0)
			{
				Summary.WriteWarning(this, String.Format("Overhead payment interval must be greater than 1 ({0})", this.Name));
				throw new Exception(String.Format("Invalid payment interval supplied for overhead {0}", this.Name));
			}

			if (StartBreedingMonth >= Clock.StartDate.Month)
			{
				NextDueDate = new DateTime(Clock.StartDate.Year, StartBreedingMonth, Clock.StartDate.Day);
			}
			else
			{
				NextDueDate = new DateTime(Clock.StartDate.Year, StartBreedingMonth, Clock.StartDate.Day);
				while (Clock.StartDate > NextDueDate)
				{
					NextDueDate = NextDueDate.AddMonths(BreedingInterval);
				}
			}
		}

		/// <summary>An event handler to perform herd breeding </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalBreeding")]
		private void OnWFAnimalBreeding(object sender, EventArgs e)
		{
			if (this.NextDueDate.Month == Clock.Today.Month)
			{
				double malebreeders = SelectedOtherAnimalsType.Cohorts.Where(a => a.Age >= this.BreedingAge & a.Gender == Sex.Male).Sum(b => b.Number);
				if (!UseLocalMales ^ malebreeders > 0)
				{
					// get nuber of females
					double breeders = SelectedOtherAnimalsType.Cohorts.Where(a => a.Age >= this.BreedingAge & a.Gender == Sex.Female).Sum(b => b.Number);
					// create new cohorts (male and female)
					if (breeders > 0)
					{
						double newbysex = breeders * this.OffspringPerBreeder / 2.0;
						OtherAnimalsTypeCohort newmales = new OtherAnimalsTypeCohort()
						{
							Age = 0,
							Weight = 0,
							Gender = Sex.Male,
							Number = newbysex,
							SaleFlag = Common.HerdChangeReason.Born
						};
						SelectedOtherAnimalsType.Add(newmales, this.Name, SelectedOtherAnimalsType.Name);
						OtherAnimalsTypeCohort newfemales = new OtherAnimalsTypeCohort()
						{
							Age = 0,
							Weight = 0,
							Gender = Sex.Female,
							Number = newbysex,
							SaleFlag = Common.HerdChangeReason.Born
						};
						SelectedOtherAnimalsType.Add(newfemales, this.Name, SelectedOtherAnimalsType.Name);
					}
				}
				this.NextDueDate = this.NextDueDate.AddMonths(this.BreedingInterval);
			}
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			// this activity is performed in WFAnimalBreeding event
			throw new NotImplementedException();
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns></returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			// determine labour for animal breeding and request it.

			return null;
		}

		/// <summary>
		/// Resource shortfall event handler
		/// </summary>
		public override event EventHandler ResourceShortfallOccurred;

		/// <summary>
		/// Shortfall occurred 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnShortfallOccurred(EventArgs e)
		{
			if (ResourceShortfallOccurred != null)
				ResourceShortfallOccurred(this, e);
		}
	}
}
