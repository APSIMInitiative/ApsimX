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
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity will arange payment for a task based on the labour specified in the labour requirement.")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourTask.htm")]
    [Version(1, 0, 1, "")]
    public class LabourActivityTask : CLEMActivityBase
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityTask()
        {
            this.SetDefaults();
            TransactionCategory = "Labour.[Task]";
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> resourcesNeeded = null;
            return resourcesNeeded;
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            return;
        }

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            // get all days required as fixed only option from requirement
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    return new GetDaysLabourRequiredReturnArgs(requirement.LabourPerUnit, TransactionCategory, null);
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
        }

        /// <inheritdoc/>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <inheritdoc/>
        public override event EventHandler ResourceShortfallOccurred;

        /// <inheritdoc/>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public override event EventHandler ActivityPerformed;

        /// <inheritdoc/>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "\r\n<div class=\"activityentry\">This activity uses a category label ";
            if (TransactionCategory != null && TransactionCategory != "")
            {
                html += "<span class=\"setvalue\">" + TransactionCategory + "</span> ";
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
