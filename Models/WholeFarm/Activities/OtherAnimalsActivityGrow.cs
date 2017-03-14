using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Activities
{
	/// <summary>Other animals grow activity</summary>
	/// <summary>This activity grows other animals and includes aging</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	public class OtherAnimalsActivityGrow : WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Name of Other Animal Type
		/// </summary>
		[Description("Name of Other Animal Type")]
		public string OtherAnimalType { get; set; }

		private OtherAnimalsType animalType { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// locate OtherAnimalsType resource
			bool resourceAvailable = false;
			animalType = Resources.GetResourceItem("OtherAnimals", OtherAnimalType, out resourceAvailable) as OtherAnimalsType;
		}

		/// <summary>
		/// Function to age other animals
		/// This needs to be undertaken prior to herd management
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAgeResources")]
		private void OnWFAgeResources(object sender, EventArgs e)
		{
			// grow all individuals
			foreach (OtherAnimalsTypeCohort cohort in animalType.Cohorts.Where(a => a.GetType() == typeof(OtherAnimalsTypeCohort)))
			{
				cohort.Age++;
			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>A list of resource requests</returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			return null;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			return;
		}

		/// <summary>
		/// res sh
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
