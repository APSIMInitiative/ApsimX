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
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [Description("This activity timer defines a range between months upon which to perform activities.")]
    public class ActivityTimerMonthRange: WFModel, IActivityTimer
	{
		[XmlIgnore]
		[Link]
		Clock Clock = null;

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
		public bool ActivityDue
		{
            get
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
		}

	}
}
