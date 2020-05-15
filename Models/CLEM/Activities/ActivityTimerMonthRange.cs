using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity timer based on monthly interval
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [Description("This activity timer defines a range between months upon which to perform activities.")]
    [HelpUri(@"Content/Features/Timers/MonthRange.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerMonthRange: CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [XmlIgnore]
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Notify CLEM that this Timer was performed
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Start month of annual period to perform activities
        /// </summary>
        [Description("Start month of annual period to perform activities (1-12)")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Month]
        public int StartMonth { get; set; }
        /// <summary>
        /// End month of annual period to perform activities
        /// </summary>
        [Description("End month of annual period to perform activities (1-12)")]
        [Required, Month]
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
                return IsMonthInRange(Clock.Today);
            }
        }

        /// <summary>
        /// Method to determine whether the activity is due based on a specified date
        /// </summary>
        /// <returns>Whether the activity is due based on the specified date</returns>
        public bool Check(DateTime dateToCheck)
        {
            return IsMonthInRange(dateToCheck);
        }

        private bool IsMonthInRange(DateTime date)
        {
            bool due = false;
            if (StartMonth <= EndMonth)
            {
                if ((date.Month >= StartMonth) & (date.Month <= EndMonth))
                {
                    due = true;
                }
            }
            else
            {
                if ((date.Month <= EndMonth) | (date.Month >= StartMonth))
                {
                    due = true;
                }
            }
            return due;
        }

        /// <summary>
        /// Activity has occurred 
        /// </summary>
        /// <param name="e"></param>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"filter\">";
            html += "Perform between ";
            if (StartMonth == 0)
            {
                html += "<span class=\"errorlink\">NOT SET</span>";
            }
            else
            {
                html += "<span class=\"setvalueextra\">";
                html += new DateTime(2000, StartMonth, 1).ToString("MMMM") + "</span>";
            }
            html += " and <span class=\"setvalueextra\">";
            if (EndMonth == 0)
            {
                html += "<span class=\"errorlink\">NOT SET</span>";
            }
            else
            {
                html += "<span class=\"setvalueextra\">";
                html += new DateTime(2000, EndMonth, 1).ToString("MMMM") + "</span>";
            }
            html += "</div>";
            if (!this.Enabled)
            {
                html += " - DISABLED!";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + "\">";
            return html;
        }
    }
}
