using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Activities
{
	/// <summary>Date range timing control for activity</summary>
	/// <summary>This activity determines if child activities will be performed.</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class TimingActivityDateRange : WFActivityBase
	{
		[Link]
		Clock Clock = null;
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// Start date of period to perform activities
		/// </summary>
		[Description("Start date of period to perform activities")]
		public DateTime StartDate { get; set; }
		/// <summary>
		/// Start date of period to perform activities
		/// </summary>
		[Description("End date of period to perform activities")]
		public DateTime EndDate { get; set; }

		private DateTime startDate;
		private DateTime endDate;

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			if(StartDate >= EndDate)
			{
				string error = String.Format("Start date must be less than end date in ({0})", this.Name);
				Summary.WriteWarning(this, error);
				throw new Exception(error);
			}
			endDate = new DateTime(EndDate.Year, EndDate.Month, DateTime.DaysInMonth(EndDate.Year, EndDate.Month));
			startDate = new DateTime(StartDate.Year, StartDate.Month, 1);
		}

		private bool IsInRange()
		{
			if((Clock.Today>=startDate)&&(Clock.Today <= endDate))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Method to test timing and cascade calls for resources for all activities in the UI tree. 
		/// Responds to WFInitialiseActivity in the Activity model holding top level list of activities
		/// </summary>
		public override void GetResourcesForAllActivityInitialisation()
		{
			if(IsInRange())
			{
				this.ResourcesForAllActivityInitialisation();
			}
		}

		/// <summary>
		/// Method to cascade calls for resources for all activities in the UI tree. 
		/// Responds to WFGetResourcesRequired in the Activity model holing top level list of activities
		/// </summary>
		public override void GetResourcesForAllActivities()
		{
			if (IsInRange())
			{
				ResourcesForAllActivities();
			}
		}

		/// <summary>
		/// Method to get required resources for initialisation of this activity. 
		/// </summary>
		public override void GetResourcesRequiredForInitialisation()
		{
			if (IsInRange())
			{
				ResourcesRequiredForInitialisation();
			}
		}

		/// <summary>
		/// Method to get this time steps current required resources for this activity. 
		/// </summary>
		public override void GetResourcesRequiredForActivity()
		{
			if (IsInRange())
			{
				ResourcesRequiredForActivity();
			}
		}




		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns></returns>
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

	}

}
