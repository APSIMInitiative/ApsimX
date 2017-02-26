using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	/// <summary>Enterprise overhead</summary>
	/// <summary>This is an overhead cost for running the enterprise</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class EnterpriseOverhead: Model
	{
		/// <summary>
		/// Get the Clock.
		/// </summary>
		[XmlIgnore]
		[Link]
		Clock Clock = null;

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
		[Description("First month to pay overhead (1-12)")]
		public int MonthDue { get; set; }

		/// <summary>
		/// Amount payable
		/// </summary>
		[Description("Amount payable")]
		public double Amount { get; set; }

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
			if(PaymentInterval <= 0)
			{
				Summary.WriteWarning(this, String.Format("Overhead payment interval must be greater than 1 ({0})", this.Name));
				throw new Exception(String.Format("Invalid payment interval supplied for overhead {0}", this.Name));
			}

			if(MonthDue >= Clock.StartDate.Month)
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

	}
}
