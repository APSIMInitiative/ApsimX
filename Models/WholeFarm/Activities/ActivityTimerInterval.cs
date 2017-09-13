using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>
	/// Activity timer based on monthly interval
	/// </summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class ActivityTimerInterval: WFModel, IActivityTimer
	{
		[XmlIgnore]
		[Link]
		Clock Clock = null;
		[XmlIgnore]
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// The payment interval (in months, 1 monthly, 12 annual)
		/// </summary>
		[System.ComponentModel.DefaultValueAttribute(12)]
		[Description("The interval (in months, 1 monthly, 12 annual)")]
        [Required]
        public int Interval { get; set; }

		/// <summary>
		/// First month to pay overhead
		/// </summary>
		[System.ComponentModel.DefaultValueAttribute(1)]
		[Description("First month to start interval (1-12)")]
        [Required]
        public int MonthDue { get; set; }

		/// <summary>
		/// Month this overhead is next due.
		/// </summary>
		[XmlIgnore]
		public DateTime NextDueDate { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ActivityTimerInterval()
		{
			this.SetDefaults();
		}

		/// <summary>
		/// Method to determine whether the activity is due
		/// </summary>
		/// <returns>Whether the activity is due in the current month</returns>
		public bool ActivityDue()
		{
			return (this.NextDueDate.Year == Clock.Today.Year & this.NextDueDate.Month == Clock.Today.Month);
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("EndOfMonth")]
		private void OnEndOfMonth(object sender, EventArgs e)
		{
			if (this.ActivityDue())
			{
				NextDueDate = NextDueDate.AddMonths(Interval);
			}
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// check payment interval > 0
			if (Interval <= 0)
			{
				Summary.WriteWarning(this, String.Format("Timer interval must be greater than 1 ({0})", this.Name));
				throw new Exception(String.Format("Invalid timer interval supplied for {0}", this.Name));
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
					NextDueDate = NextDueDate.AddMonths(Interval);
				}
			}

		}



	}
}
