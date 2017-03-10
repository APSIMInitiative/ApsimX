using Models.Core;
using Models.WholeFarm.Activities;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Groupings
{
	///<summary>
	/// Contains a group of filters to identify individual ruminants to muster
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantActivityMuster))]
	public class RuminantMusterGroup: WFActivityBase
	{
		[Link]
		Clock Clock = null;
		[Link]
		private ResourcesHolder Resources = null;
		[Link]
		private Activities.ActivitiesHolder Activities =  null;
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// Name of managed pasture to muster to
		/// </summary>
		[Description("Name of managed pasture to muster to")]
		public string ManagedPastureName { get; set; }

		/// <summary>
		/// Determines whether mustering happens if insufficient labour
		/// </summary>
		[Description("Muster if labour shortfall")]
		public bool MusterOnLabourShortfall { get; set; }

		/// <summary>
		/// Determines whether this must be performed to setup herds at the start of the simulation
		/// </summary>
		[Description("Perform muster at start of simulation")]
		public bool PerformAtStartOfSimulation { get; set; }

		/// <summary>
		/// Month to muster in (set to 0 to not perform muster)
		/// </summary>
		[Description("Month to muster in")]
		public int Month { get; set; }

		/// <summary>
		/// Determines whether sucklings are automatically mustered with the mother or seperated
		/// </summary>
		[Description("Move sucklings with mother")]
		public bool MoveSucklings { get; set; }

		/// <summary>
		/// Labour required per x head
		/// </summary>
		[Description("Labour required per x head")]
		public double LabourRequired { get; set; }

		/// <summary>
		/// Number of head per labour unit required
		/// </summary>
		[Description("Number of head per labour unit required")]
		public double LabourHeadUnit { get; set; }

		/// <summary>
		/// Labour grouping for breeding
		/// </summary>
		public List<object> LabourFilterList { get; set; }

		private PastureActivityManage pasture { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("StartOfSimulation")]
		private void OnStartOfSimulation(object sender, EventArgs e)
		{
			// This needs to happen after all manage pasture activities have been initialised on commencing
			// Therefore we use StartOfSimulation event

			// link to pasture to muster to
			pasture = Activities.GetByName(ManagedPastureName) as PastureActivityManage;

			if (pasture == null)
			{
				Summary.WriteWarning(this, String.Format("Could not find manage pasture activity named \"{0}\" for {1}", ManagedPastureName, this.Name));
				throw new Exception(String.Format("Invalid pasture name ({0}) provided for mustering activity {1}", ManagedPastureName, this.Name));
			}

			if (LabourRequired > 0)
			{
				// check for and assign labour filter group
				LabourFilterList = this.Children.Where(a => a.GetType() == typeof(LabourFilterGroup)).Cast<object>().ToList();
				// if not present assume can use any labour and report
				if (LabourFilterList == null)
				{
					Summary.WriteWarning(this, String.Format("No labour filter details provided for feeding activity ({0}). Assuming any labour type can be used", this.Name));
					LabourFilterGroup lfg = new LabourFilterGroup();
					LabourFilter lf = new LabourFilter()
					{
						Operator = FilterOperators.GreaterThanOrEqual,
						Value = "0",
						Parameter = LabourFilterParameters.Age
					};
					lfg.Children.Add(lf);
					LabourFilterList = new List<object>();
					LabourFilterList.Add(lfg);
				}
			}

			if (PerformAtStartOfSimulation)
			{
				Muster();
			}
		}

		private void Muster()
		{
			// get herd to muster
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;

			if (herd == null && herd.Count == 0) return;

			// get list from filters
			foreach (Ruminant ind in herd.Filter(this))
			{
				// set new location ID
				ind.Location = pasture.Name;

				// check if sucklings are to be moved with mother
				if (MoveSucklings)
				{
					// if female
					if (ind.GetType() == typeof(RuminantFemale))
					{
						RuminantFemale female = ind as RuminantFemale;
						// check if mother with sucklings
						if (female.SucklingOffspring.Count > 0)
						{
							foreach (var suckling in female.SucklingOffspring)
							{
								suckling.Location = pasture.Name;
							}
						}
					}
				}

			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			ResourceRequestList = null;
			if (Clock.Today.Month == Month)
			{
				// if labour item(s) found labour will be requested for this activity.
				if (LabourRequired > 0)
				{
					if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
					// determine head to be mustered
					RuminantHerd ruminantHerd = Resources.RuminantHerd();
					List<Ruminant> herd = ruminantHerd.Herd;
					int head = herd.Filter(this).Count();
					ResourceRequestList.Add(new ResourceRequest()
					{
						AllowTransmutation = true,
						Required = Math.Ceiling(head / this.LabourHeadUnit) * this.LabourRequired,
						ResourceName = "Labour",
						ResourceTypeName = "",
						ActivityName = this.Name,
						FilterDetails = LabourFilterList
					}
					);
				}
			}
			return ResourceRequestList;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			if (Clock.Today.Month == Month)
			{
				// move individuals
				Muster();
			}
		}
	}
}
