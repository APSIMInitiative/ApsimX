using Models.Core;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
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
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// Get the Clock.
		/// </summary>
		[Link]
		Clock Clock = null;

		/// <summary>
		/// Daily labour rate
		/// </summary>
		[Description("Daily labour rate")]
		public double DailyRate { get; set; }

		/// <summary>
		/// Days worked
		/// </summary>
		[Description("Days work available each month")]
		public double[] DaysWorkAvailableEachMonth { get; set; }

		/// <summary>
		/// Bank account name to pay to
		/// </summary>
		[Description("Bank account name to pay to")]
		public string BankAccountName { get; set; }

		private FinanceType bankType { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// locate BankType resource
			bool resourceAvailable = false;
			bankType = Resources.GetResourceItem("Finances", BankAccountName, out resourceAvailable) as FinanceType;

			if(this.Children.Where(a => a.GetType() == typeof(LabourFilterGroup)).Count() > 1)
			{
				Summary.WriteWarning(this, String.Format("Only one Labour Filter Group can be provied for off farm labour. The first filter group will be used for {0}", this.Name));
			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns></returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			ResourceRequestList = new List<ResourceRequest>();

			// zero based month index for array
			int month = Clock.Today.Month - 1;

			if (DaysWorkAvailableEachMonth[month] > 0)
			{
				ResourceRequestList.Add(new ResourceRequest()
				{
					AllowTransmutation = false,
					Required = DaysWorkAvailableEachMonth[month],
					ResourceName = "Labour",
					ResourceTypeName = "",
					ActivityName = this.Name,
					Reason = this.Name,
					FilterDetails = this.Children.Where(a => a.GetType() == typeof(LabourFilterGroup)).ToList<object>()
				}
				);
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
		public override void PerformActivity()
		{
			// days provided from labour set in the only request in the resourceResquestList
			// receive payment for labour if bank type exists
			if (bankType != null)
			{
				bankType.Add(ResourceRequestList.FirstOrDefault().Available * DailyRate, "Off farm labour", this.Name);
			}
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
