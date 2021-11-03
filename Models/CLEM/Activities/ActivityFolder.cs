using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Activities
{
    /// <summary>An folder to manage activity grouping</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity folder holds groups of activities and is used to arrange activities and apply timers to the group")]
    [HelpUri(@"Content/Features/Activities/ActivitiesFolder.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityFolder : CLEMActivityBase
    {
        /// <inheritdoc/>
        public new string TransactionCategory { get; set; }

        /// <inheritdoc/>
        [JsonIgnore]
        public new OnPartialResourcesAvailableActionTypes OnPartialResourcesAvailableAction { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityFolder()
        {
            TransactionCategory = "Folder";
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            return "\r\n<div class=folder>" + this.Name + " folder " + ((!this.Enabled) ? " - DISABLED!" : "") + "</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "\r\n</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "\r\n<div class=\"activityborder\" style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + ";\">";
        } 
        #endregion

    }
}
