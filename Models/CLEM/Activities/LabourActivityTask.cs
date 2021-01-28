using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Models.CLEM;
using Models.CLEM.Groupings;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Labour activity task</summary>
    /// <summary>Defines a labour activity task with associated costs</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity will arange payment for a task based on the labour specified in the labour requirement.")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourTask.htm")]
    [Version(1, 0, 1, "")]
    public class LabourActivityTask : CLEMActivityBase, ICategoryActivity
    {
        /// <summary>
        /// Category label to use in ledger
        /// </summary>
        [Description("Shortname of task for reporting")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Shortname required")]
        public string Category { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityTask()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> resourcesNeeded = null;
            return resourcesNeeded;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return;
        }

        /// <summary>
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="requirement">Labour requirement model</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            // get all days required as fixed only option from requirement
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    return new GetDaysLabourRequiredReturnArgs(requirement.LabourPerUnit, this.Category, null);
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary
        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "\r\n<div class=\"activityentry\">This activity uses a category label ";
            if (Category != null && Category != "")
            {
                html += "<span class=\"setvalue\">" + Category + "</span> ";
            }
            else
            {
                html += "<span class=\"errorlink\">[NOT SET]</span> ";
            }
            html += " for all transactions</div>";
            return html;
        } 
        #endregion
    }
}
