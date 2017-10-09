using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant graze activity</summary>
	/// <summary>This activity determines how a ruminant group will graze</summary>
	/// <summary>It is designed to request food via a food store arbitrator</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs grazing of all herds and pastures (paddocks) in the simulation.")]
    public class RuminantActivityGrazeAll : WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Number of hours grazed
		/// Based on 8 hour grazing days
		/// Could be modified to account for rain/heat walking to water etc.
		/// </summary>
		[Description("Number of hours grazed")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day")]
        public double HoursGrazed { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// create activity for each pasture type and breed at startup
			foreach (GrazeFoodStoreType pastureType in Resources.GrazeFoodStore().Children)
			{
				RuminantActivityGrazePasture ragp = new RuminantActivityGrazePasture();
				ragp.GrazeFoodStoreModel = pastureType;

				foreach (RuminantType herdType in Resources.RuminantHerd().Children)
				{
					RuminantActivityGrazePastureBreed ragpb = new RuminantActivityGrazePastureBreed();
					ragpb.GrazeFoodStoreModel = pastureType;
					ragpb.RuminantTypeModel = herdType;
					ragpb.HoursGrazed = HoursGrazed;
					ragp.ActivityList.Add(ragpb);
				}
				ActivityList.Add(ragp);
			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> GetResourcesNeededForActivity()
		{
			return null;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void DoActivity()
		{
			return;
		}

		/// <summary>
		/// Method to determine resources required for initialisation of this activity
		/// </summary>
		/// <returns></returns>
		public override List<ResourceRequest> GetResourcesNeededForinitialisation()
		{
			return null;
		}

		/// <summary>
		/// Method used to perform initialisation of this activity.
		/// This will honour ReportErrorAndStop action but will otherwise be preformed regardless of resources available
		/// It is the responsibility of this activity to determine resources provided.
		/// </summary>
		public override void DoInitialisation()
		{
			return;
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

		/// <summary>
		/// Resource shortfall occured event handler
		/// </summary>
		public override event EventHandler ActivityPerformed;

		/// <summary>
		/// Shortfall occurred 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnActivityPerformed(EventArgs e)
		{
			if (ActivityPerformed != null)
				ActivityPerformed(this, e);
		}


	}
}
