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
    [ValidParent(ParentType = typeof(RuminantActivityTrade))]
    [ValidParent(ParentType = typeof(RuminantActivityWean))]
    [ValidParent(ParentType = typeof(TransmuteRuminant))]
    [ValidParent(ParentType = typeof(ReportRuminantAttributeSummary))]
    [Description("Selects specific individuals ruminants from the herd")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "Added ability to select random proportion of the group to use")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantGroup.htm")]
    public class RuminantGroup : FilterGroup<Ruminant>, IValidatableObject, IIdentifiableChildModel
    {
        /// <summary>
        /// An identifier for this FilterGroup based on parent requirements
        /// </summary>
        [Description("Group identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers")]
        public string Identifier { get; set; }

        /// <inheritdoc/>
        [XmlIgnore]
        public string Units 
        { 
            get { return ""; }
            set { units = value; }
        }
        private string units;          

        /// <inheritdoc/>
        [XmlIgnore]
        public bool ShortfallAffectsActivity { get; set; }

        /// <summary>
        /// Constructor to apply defaults
        /// </summary>
        public RuminantGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
            this.SetDefaults();
        }

        /// <inheritdoc/>
        public List<ResourceRequest> GetResourceRequests(double activityMetric)
        {
            return new List<ResourceRequest>();
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

        #region validation

        /// <summary>
        /// A method to return the list of identifiers relavent to this parent activity
        /// </summary>
        /// <returns>A list of identifiers</returns>
        public List<string> ParentSuppliedIdentifiers()
        {
            if (Parent != null && Parent is ICanHandleIdentifiableChildModels)
                return (Parent as ICanHandleIdentifiableChildModels).DefineIdentifiableChildModelLabels<RuminantGroup>().Identifiers;
            else
                return new List<string>();
        }

        /// <summary>
        /// A method to return the list of units relavent to this parent activity
        /// </summary>
        /// <returns>A list of units</returns>
        public List<string> ParentSuppliedUnits()
        {
            if (Parent != null && Parent is ICanHandleIdentifiableChildModels)
                return (Parent as ICanHandleIdentifiableChildModels).DefineIdentifiableChildModelLabels<RuminantGroup>().Units;
            else
                return new List<string>();
        }

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Parent != null && Parent is ICanHandleIdentifiableChildModels)
            {
                var identifiers = ParentSuppliedIdentifiers();
                if (identifiers.Any() & !identifiers.Contains(Identifier))
                {
                    string[] memberNames = new string[] { "Ruminant group" };
                    results.Add(new ValidationResult($"The identifier [{(((Identifier??"") == "") ? "BLANK" : Identifier)}] in [f={this.Name}] is not valid for the parent activity [a={Parent.Name}].{Environment.NewLine}Select an option from the list. If the list is empty this activity does not support custom ruminant filtering", memberNames));
                }
            }
            return results;
        }
        #endregion

    }
}