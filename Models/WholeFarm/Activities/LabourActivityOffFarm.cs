using Models.Core;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>
	/// Off farm labour activities
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class LabourActivityOffFarm: WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Get the Clock.
		/// </summary>
		[Link]
		Clock Clock = null;

		/// <summary>
		/// Daily labour rate
		/// </summary>
		[Description("Daily labour rate")]
        [Required]
        public double DailyRate { get; set; }

		/// <summary>
		/// Days worked
		/// </summary>
		[Description("Days work available each month")]
        [Required]
        public double[] DaysWorkAvailableEachMonth { get; set; }

		/// <summary>
		/// Bank account name to pay to
		/// </summary>
		[Description("Bank account name to pay to")]
        [Required]
        public string BankAccountName { get; set; }

		private FinanceType bankType { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("WFInitialiseActivity")]
        private void OnWFInitialiseActivity(object sender, EventArgs e)
        {
            // locate BankType resource
            bankType = Resources.GetResourceItem(this, typeof(Finance), BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as FinanceType;
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns></returns>
		public override List<ResourceRequest> GetResourcesNeededForActivity()
		{
			// zero based month index for array
			int month = Clock.Today.Month - 1;

			if (DaysWorkAvailableEachMonth[month] > 0)
			{
				foreach (LabourFilterGroup filter in Apsim.Children(this, typeof(LabourFilterGroup)))
				{
					if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
					ResourceRequestList.Add(new ResourceRequest()
					{
						AllowTransmutation = false,
						Required = DaysWorkAvailableEachMonth[month],
						ResourceType = typeof(Labour),
						ResourceTypeName = "",
						ActivityModel = this,
						Reason = this.Name,
						FilterDetails = new List<object>() { filter }// filter.ToList<object>() // this.Children.Where(a => a.GetType() == typeof(LabourFilterGroup)).ToList<object>()
					}
					);
				}
			}
			else
			{
				return null;
			}
			return ResourceRequestList;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void DoActivity()
		{
			// days provided from labour set in the only request in the resourceResquestList
			// receive payment for labour if bank type exists
			if (bankType != null)
			{
				bankType.Add(ResourceRequestList.FirstOrDefault().Available * DailyRate, "Off farm labour", this.Name);
			}
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
