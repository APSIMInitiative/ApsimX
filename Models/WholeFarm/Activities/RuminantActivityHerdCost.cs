using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.WholeFarm;
using Models.WholeFarm.Groupings;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant herd cost </summary>
	/// <summary>This activity will arrange payment of a herd expense such as vet fees</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	public class RuminantActivityHerdCost : WFActivityBase
	{
		/// <summary>
		/// Get the Clock.
		/// </summary>
		[XmlIgnore]
		[Link]
		Clock Clock = null;
		[Link]
		private ResourcesHolder Resources = null;

		[XmlIgnore]
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// The payment interval (in months, 1 monthly, 12 annual)
		/// </summary>
		[Description("The payment interval (in months, 1 monthly, 12 annual)")]
		public int PaymentInterval { get; set; }

		/// <summary>
		/// First month to pay overhead
		/// </summary>
		[Description("First month to pay expense (1-12)")]
		public int MonthDue { get; set; }

		/// <summary>
		/// Amount payable
		/// </summary>
		[Description("Amount payable")]
		public double Amount { get; set; }

		/// <summary>
		/// Payment style
		/// </summary>
		[Description("Payment style")]
		public Common.HerdPaymentStyleType PaymentStyle { get; set; }

		/// <summary>
		/// name of account to use
		/// </summary>
		[Description("Name of account to use")]
		public string AccountName { get; set; }

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
			// check payment interval > 0
			if (PaymentInterval <= 0)
			{
				Summary.WriteWarning(this, String.Format("Overhead payment interval must be greater than 1 ({0})", this.Name));
				throw new Exception(String.Format("Invalid payment interval supplied for overhead {0}", this.Name));
			}

			if (MonthDue >= Clock.StartDate.Month)
			{
				NextDueDate = new DateTime(Clock.StartDate.Year, MonthDue, Clock.StartDate.Day);
			}
			else
			{
				NextDueDate = new DateTime(Clock.StartDate.Year, MonthDue, Clock.StartDate.Day);
				while (Clock.StartDate > NextDueDate)
				{
					NextDueDate = NextDueDate.AddMonths(PaymentInterval);
				}
			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			ResourceRequestList = new List<ResourceRequest>();

			if (this.NextDueDate.Year == Clock.Today.Year & this.NextDueDate.Month == Clock.Today.Month)
			{
				double amountNeeded = 0;
				switch (PaymentStyle)
				{
					case Common.HerdPaymentStyleType.Fixed:
						amountNeeded = Amount;
						break;
					case Common.HerdPaymentStyleType.perHead:
						List<Ruminant> herd = Resources.RuminantHerd().Herd;
						// check for Ruminant filter group
						if(this.Children.Where(a => a.GetType() == typeof(RuminantFilterGroup)).Count() > 0)
						{
							herd = herd.Filter(this.Children.Where(a => a.GetType() == typeof(RuminantFilterGroup)).FirstOrDefault());
						}
						amountNeeded = Amount*herd.Count();
						break;
					case Common.HerdPaymentStyleType.perAE:
						herd = Resources.RuminantHerd().Herd;
						// check for Ruminant filter group
						if (this.Children.Where(a => a.GetType() == typeof(RuminantFilterGroup)).Count() > 0)
						{
							herd = herd.Filter(this.Children.Where(a => a.GetType() == typeof(RuminantFilterGroup)).FirstOrDefault());
						}
						amountNeeded = Amount * herd.Sum(a => a.AdultEquivalent);
						break;
					default:
						throw new Exception(String.Format("Unknown Payment style {0} in {1}",PaymentStyle, this.Name));
				}

				ResourceRequestList.Add(new ResourceRequest()
				{
					AllowTransmutation = false,
					Required = amountNeeded,
					ResourceName = "Finances",
					ResourceTypeName = this.AccountName,
					ActivityName = this.Name,
					Reason = "AddBreedHere"
				}
				);
			}
			return ResourceRequestList;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			// if occurred
			if (this.NextDueDate.Year == Clock.Today.Year & this.NextDueDate.Month == Clock.Today.Month)
			{
				ResourceRequest thisRequest = ResourceRequestList.FirstOrDefault();
				if (thisRequest != null)
				{
					// update next due date
					this.NextDueDate = this.NextDueDate.AddMonths(this.PaymentInterval);
				}
			}
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

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> DetermineResourcesNeeded()
		{
			return null;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void PerformActivity()
		{
			return; ;
		}
	}
}
