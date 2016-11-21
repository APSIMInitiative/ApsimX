using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	///<summary>
	/// Contains a group of filters to identify individual ruminants to muster
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantActivityMuster))]
	public class RuminantMusterGroup: Model
	{
		[Link]
		Clock Clock = null;
		[Link]
		private Resources Resources = null;
		[Link]
		private Activities Activities =  null;
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
		/// Determines whether this must is performed to setup herds at the start of the simulation
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

		private bool labourIncluded = false;

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

		/// <summary>An event handler to call for all resources other than food for feeding activity</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFRequestResources")]
		private void OnWFRequestResources(object sender, EventArgs e)
		{
			if (Clock.Today.Month == Month)
			{
				// if labour item(s) found labour will be requested for this activity.
				labourIncluded = false;
				// check labour required

				// request labour
			}
		}

		/// <summary>An event handler to call for all resources other than food for feeding activity</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFResourcesAllocated")]
		private void OnWFResourcesAllocated(object sender, EventArgs e)
		{
			if (Clock.Today.Month == Month)
			{

				bool labourShortfall = false;
				if (labourIncluded)
				{


				}

				if (!labourShortfall | this.MusterOnLabourShortfall)
				{
					// move individuals
					Muster();
				}
			}
		}


	}
}
