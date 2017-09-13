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
	public class ActivityTimerMonthRange: WFModel, IActivityTimer
	{
		[XmlIgnore]
		[Link]
		Clock Clock = null;
		[XmlIgnore]
		[Link]
		ISummary Summary = null;

		/// <summary>
		/// Start month of annual period to perform activities
		/// </summary>
		[Description("Start month of annual period to perform activities (1-12)")]
		[System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Range(1, 12, ErrorMessage = "Value must represent a month from 1 (Jan) to 12 (Dec)")]
        public int StartMonth { get; set; }
		/// <summary>
		/// End month of annual period to perform activities
		/// </summary>
		[Description("End month of annual period to perform activities (1-12)")]
        [Required, Range(1, 12, ErrorMessage = "Value must represent a month from 1 (Jan) to 12 (Dec)")]
        [System.ComponentModel.DefaultValueAttribute(12)]
		public int EndMonth { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public ActivityTimerMonthRange()
		{
			this.SetDefaults();
		}

		/// <summary>
		/// Method to determine whether the activity is due
		/// </summary>
		/// <returns>Whether the activity is due in the current month</returns>
		public bool ActivityDue()
		{
			if (StartMonth < EndMonth)
			{
				if ((Clock.Today.Month >= StartMonth) && (Clock.Today.Month <= EndMonth))
				{
					return true;
				}
				return false;
			}
			else
			{
				if ((Clock.Today.Month >= EndMonth) | (Clock.Today.Month <= StartMonth))
				{
					return true;
				}
				return false;
			}
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			if ((StartMonth < 1) | (StartMonth > 12))
			{
				string error = String.Format("Start month must be a value between 1 and 12 in ({0})", this.Name);
				Summary.WriteWarning(this, error);
				throw new Exception(error);
			}
			if ((EndMonth < 1) | (EndMonth > 12))
			{
				string error = String.Format("End month must be a value between 1 and 12 in ({0})", this.Name);
				Summary.WriteWarning(this, error);
				throw new Exception(error);
			}
		}

	}
}
