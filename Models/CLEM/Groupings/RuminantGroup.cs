using Models.Core;
using Models.CLEM.Activities;
using Models.CLEM.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using Newtonsoft.Json;
using Models.CLEM.Resources;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Models.CLEM.Interfaces;
using System.Xml.Serialization;
using Models.CLEM.Timers;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters and sorters to identify individual ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ReportRuminantHerd))]
    [ValidParent(ParentType = typeof(SummariseRuminantHerd))]
    [ValidParent(ParentType = typeof(RuminantActivityControlledMating))]
    [ValidParent(ParentType = typeof(RuminantActivityFeed))]
    [ValidParent(ParentType = typeof(RuminantActivityHerdCost))]
    [ValidParent(ParentType = typeof(RuminantActivityManage))]
    [ValidParent(ParentType = typeof(RuminantActivityMarkForSale))]
    [ValidParent(ParentType = typeof(RuminantActivityMilking))]
    [ValidParent(ParentType = typeof(RuminantActivityMove))]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStocking))]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStockingENSO))]
    [ValidParent(ParentType = typeof(RuminantActivityShear))]
    [ValidParent(ParentType = typeof(RuminantActivityTag))]
    [ValidParent(ParentType = typeof(RuminantActivityPurchase))]
    [ValidParent(ParentType = typeof(RuminantActivityWean))]
    [ValidParent(ParentType = typeof(TransmuteRuminant))]
    [ValidParent(ParentType = typeof(ReportRuminantAttributeSummary))]
    [ValidParent(ParentType = typeof(ActivityTimerRuminantLevel))]
    [Description("Selects specific individuals ruminants from the herd")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "Added ability to select random proportion of the group to use")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantGroup.htm")]
    public class RuminantGroup : FilterGroup<Ruminant>
    {
        #region descriptive summary

            /// <inheritdoc/>
        public override string ModelSummary()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"<div class=\"filtername\" style=\"opacity: {SummaryOpacity(FormatForParentControl)}\">");
                if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                    htmlWriter.Write($"{Name}");
                if ((Identifier ?? "") != "")
                    htmlWriter.Write($" - applies to {Identifier}");

                htmlWriter.Write($"</div>");
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}