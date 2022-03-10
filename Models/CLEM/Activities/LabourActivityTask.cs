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
using System.IO;

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
    [Description("Arrange payment for a task based on the labour specified in the labour requirement")]
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
        public override List<ResourceRequest> DetermineResourcesForActivity(double argument = 0)
        {
            List<ResourceRequest> resourcesNeeded = null;
            return resourcesNeeded;
        }

        ///// <inheritdoc/>
        //protected override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        //{
        //    // get all days required as fixed only option from requirement
        //    switch (requirement.UnitType)
        //    {
        //        case LabourUnitType.Fixed:
        //            return new LabourRequiredArgs(requirement.LabourPerUnit, TransactionCategory, null);
        //        default:
        //            throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
        //    }
        //}

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">This activity uses a category label ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(TransactionCategory, "Account not set"));
                htmlWriter.Write(" for all transactions</div>");
                return htmlWriter.ToString();
            }
        } 
        #endregion
    }
}
