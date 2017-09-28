using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class OtherAnimalsActivityGrow : WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Name of Other Animal Type
		/// </summary>
		[Description("Name of Other Animal Type")]
        [Required]
        public string OtherAnimalType { get; set; }

		private OtherAnimalsType animalType { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("WFInitialiseActivity")]
        private void OnWFInitialiseActivity(object sender, EventArgs e)
        {
            // locate OtherAnimalsType resource
            animalType = Resources.GetResourceItem(this, typeof(OtherAnimals), OtherAnimalType, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as OtherAnimalsType;
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

			// death from old age
			while(animalType.Cohorts.Where(a => a.Age > animalType.MaxAge).Count() > 0)
			{
				animalType.Remove(animalType.Cohorts.Where(a => a.Age > animalType.MaxAge).FirstOrDefault(), this.Name, "Died");
			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>A list of resource requests</returns>
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
