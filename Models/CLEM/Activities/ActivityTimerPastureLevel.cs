using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity timer based on crop harvest
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [Description("This activity timer is used to determine whether a pasture biomass (t/ha) is within a specified range.")]
    [HelpUri(@"Content/Features/Timers/PastureLevel.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerPastureLevel : CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [Link]
        ResourcesHolder resources = null;

        /// <summary>
        /// Paddock or pasture to graze
        /// </summary>
        [Description("GrazeFoodStore/pasture to graze")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Graze Food Store/pasture required")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(GrazeFoodStore) } })]
        public string GrazeFoodStoreTypeName { get; set; }

        /// <summary>
        /// paddock or pasture to graze
        /// </summary>
        [JsonIgnore]
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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            GrazeFoodStoreModel = resources.FindResourceType<GrazeFoodStore, GrazeFoodStoreType>(this, GrazeFoodStoreTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <inheritdoc/>
        public bool ActivityDue
        {
            get
            {
                return (GrazeFoodStoreModel.KilogramsPerHa >= MinimumPastureLevel && GrazeFoodStoreModel.KilogramsPerHa < MaximumPastureLevel);
            }
        }

        /// <inheritdoc/>
        public bool Check(DateTime dateToCheck)
        {
            return false;
        }

        /// <inheritdoc/>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"filter\">");
                htmlWriter.Write("Perform when ");
                if (GrazeFoodStoreTypeName is null || GrazeFoodStoreTypeName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">RESOURCE NOT SET</span> ");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + GrazeFoodStoreTypeName + "</span> ");
                }
                htmlWriter.Write(" is between <span class=\"setvalueextra\">");
                htmlWriter.Write(MinimumPastureLevel.ToString());
                htmlWriter.Write("</span> and ");
                if (MaximumPastureLevel <= MinimumPastureLevel)
                {
                    htmlWriter.Write("<span class=\"resourcelink\">must be > MinimumPastureLevel</span> ");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalueextra\">");
                    htmlWriter.Write(MaximumPastureLevel.ToString());
                    htmlWriter.Write("</span> ");
                }
                htmlWriter.Write(" kg per hectare</div>");
                if (!this.Enabled)
                {
                    htmlWriter.Write(" - DISABLED!");
                }
                return htmlWriter.ToString(); 
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                {
                    htmlWriter.Write(this.Name);
                }
                htmlWriter.Write($"</div>");
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + "\">");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
