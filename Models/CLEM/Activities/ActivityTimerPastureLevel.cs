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
    /// Activity timer based on crop harvest
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [Description("This activity timer is used to determine whether a pasture biomass (t/ha) is within a specified range.")]
    [HelpUri(@"Content/Features/Timers/PastureLevel.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerPastureLevel : CLEMModel, IActivityTimer, IValidatableObject, IActivityPerformedNotifier
    {
        [Link]
        ResourcesHolder Resources = null;

        /// <summary>
        /// Paddock or pasture to graze
        /// </summary>
        [Description("GrazeFoodStore/pasture to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Graze Food Store/pasture required")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(GrazeFoodStore) })]
        public string GrazeFoodStoreTypeName { get; set; }

        /// <summary>
        /// paddock or pasture to graze
        /// </summary>
        [XmlIgnore]
        public GrazeFoodStoreType GrazeFoodStoreModel { get; set; }

        /// <summary>
        /// Minimum pasture level
        /// </summary>
        [Description("Minimum pasture level (kg/ha) >=")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumPastureLevel { get; set; }

        /// <summary>
        /// Maximum pasture level
        /// </summary>
        [Description("Maximum pasture level (kg/ha) <")]
        [Required, GreaterThan("MinimumPastureLevel", ErrorMessage ="Maximum pasture level must be greater than minimum pasture level")]
        public double MaximumPastureLevel { get; set; }

        /// <summary>
        /// Notify CLEM that this activity was performed.
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerPastureLevel()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            GrazeFoodStoreModel = Resources.GetResourceItem(this, GrazeFoodStoreTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
        }

        /// <summary>
        /// Method to determine whether the activity is due based on harvest details form parent.
        /// </summary>
        /// <returns>Whether the activity is due in the current month</returns>
        public bool ActivityDue
        {
            get
            {
                return (GrazeFoodStoreModel.KilogramsPerHa >= MinimumPastureLevel && GrazeFoodStoreModel.KilogramsPerHa < MaximumPastureLevel);
            }
        }

        /// <summary>
        /// Method to determine whether the activity is due based on a specified date
        /// </summary>
        /// <returns>Whether the activity is due based on the specified date</returns>
        public bool Check(DateTime dateToCheck)
        {
            return false;
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
            html += "Perform when ";
            if (GrazeFoodStoreTypeName is null || GrazeFoodStoreTypeName == "")
            {
                html += "<span class=\"errorlink\">RESOURCE NOT SET</span> ";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + GrazeFoodStoreTypeName + "</span> ";
            }
            html += " is between <span class=\"setvalueextra\">";
            html += MinimumPastureLevel.ToString();
            html += "</span> and ";
            if (MaximumPastureLevel <= MinimumPastureLevel)
            {
                html += "<span class=\"resourcelink\">must be > MinimumPastureLevel</span> ";
            }
            else
            {
                html += "<span class=\"setvalueextra\">";
                html += MaximumPastureLevel.ToString();
                html += "</span> ";
            }
            html += " kg per hectare</div>";
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
