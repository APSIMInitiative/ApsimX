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
	public class ActivityTimerDateRange : WFModel, IActivityTimer
	{
		[XmlIgnore]
		[Link]
		Clock Clock = null;

		/// <summary>
		/// Start date of period to perform activities
		/// </summary>
		[Description("Start date of period to perform activities")]
        [Required]
        public DateTime StartDate { get; set; }
		
		/// <summary>
		/// End date of period to perform activities
		/// </summary>
		[Description("End date of period to perform activities")]
        [Required]
        [DateGreaterThanAttribute("StartDate", ErrorMessage = "End date must be greater than Start date")]
        public DateTime EndDate { get; set; }

		/// <summary>
		/// Invert (NOT in selected range)
		/// </summary>
		[Description("Invert (NOT in selected range)")]
        [Required]
        public bool Invert { get; set; }

		private DateTime startDate;
		private DateTime endDate;

		/// <summary>
		/// Constructor
		/// </summary>
		public ActivityTimerDateRange()
		{
			this.SetDefaults();
		}

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("WFInitialiseActivity")]
        private void OnWFInitialiseActivity(object sender, EventArgs e)
        {
            endDate = new DateTime(EndDate.Year, EndDate.Month, DateTime.DaysInMonth(EndDate.Year, EndDate.Month));
			startDate = new DateTime(StartDate.Year, StartDate.Month, 1);
		}

		/// <summary>
		/// Method to determine whether the activity is due
		/// </summary>
		/// <returns>Whether the activity is due in the current month</returns>
		public bool ActivityDue
		{
            get
            {
                bool inrange = ((Clock.Today >= startDate) && (Clock.Today <= endDate));
                if (Invert)
                {
                    inrange = !inrange;
                }
                return inrange;
            }
		}
	}
}
