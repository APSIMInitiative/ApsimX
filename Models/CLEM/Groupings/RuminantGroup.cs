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
    [ValidParent(ParentType = typeof(RuminantActivityRequestPurchase))]
    [ValidParent(ParentType = typeof(RuminantActivityWean))]
    [ValidParent(ParentType = typeof(TransmuteRuminant))]
    [ValidParent(ParentType = typeof(ReportRuminantAttributeSummary))]
    [Description("Selects specific individuals ruminants from the herd")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "Added ability to select random proportion of the group to use")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantGroup.htm")]
    public class RuminantGroup : FilterGroup<Ruminant>, IActivityCompanionModel
    {
        /// <summary>
        /// An identifier for this FilterGroup based on parent requirements
        /// </summary>
        [Description("Group identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers")]
        public string Identifier { get; set; }

        /// <summary>
        /// Label to assign each transaction created by this activity child component in ledgers
        /// </summary>
        [Description("Category for transactions")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Category for transactions required")]
        [Models.Core.Display(Order = 500)]
        public virtual string TransactionCategory { get; set; }

        /// <inheritdoc/>
        [XmlIgnore]
        public string Units 
        { 
            get { return ""; }
            set { ; }
        }

        /// <inheritdoc/>
        [XmlIgnore]
        public bool ShortfallCanAffectParentActivity { get; set; }

        /// <summary>
        /// Constructor to apply defaults
        /// </summary>
        public RuminantGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
            TransactionCategory = "Select.[Ruminants]";
            this.SetDefaults();
        }

        /// <inheritdoc/>
        public virtual void PrepareForTimestep()
        {
        }

        /// <inheritdoc/>
        public virtual List<ResourceRequest> RequestResourcesForTimestep(double activityMetric)
        {
            return new List<ResourceRequest>();
        }

        /// <inheritdoc/>
        public virtual void PerformTasksForTimestep(double activityMetric)
        {
        }

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
            using StringWriter htmlWriter = new StringWriter();
            htmlWriter.Write("<div class=\"filtername\">");
            if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                htmlWriter.Write($"{Name}");
            htmlWriter.Write($"</div>");
            return htmlWriter.ToString();
        }

        #endregion

    }
}