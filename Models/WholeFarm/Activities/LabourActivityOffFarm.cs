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
		/// Amount provided from resource or arbitrator
		/// </summary>
		[XmlIgnore]
		public double AmountProvided { get; set; }

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
			// locate FeedType resource
			bool resourceAvailable = false;
			bankType = Resources.GetResourceItem("Finances", BankAccountName, out resourceAvailable) as FinanceType;
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
					Requestor = this.Name,
					FilterSortDetails = this.Children.Where(a => a.GetType() == typeof(LabourFilterGroup)).ToList<object>()
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
			// days provided from labour set in the only request ins the resourceResquestList
			// receive payment for labour
			bankType.Add(ResourceRequestList.FirstOrDefault().Available*DailyRate, this.Name, this.Name);
		}
	}
}
