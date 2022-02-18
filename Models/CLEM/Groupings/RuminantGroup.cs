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
    [ValidParent(ParentType = typeof(RuminantActivityManage))]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStocking))]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStockingENSO))]
    [ValidParent(ParentType = typeof(RuminantActivityMove))]
    [ValidParent(ParentType = typeof(RuminantActivityMarkForSale))]
    [ValidParent(ParentType = typeof(RuminantActivityWean))]
    [ValidParent(ParentType = typeof(RuminantActivityTag))]
    [ValidParent(ParentType = typeof(TransmuteRuminant))]
    [ValidParent(ParentType = typeof(ReportRuminantAttributeSummary))]
    [Description("Selects specific individuals ruminants from the herd")]
    [Version(1, 0, 1, "Added ability to select random proportion of the group to use")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantGroup.htm")]
    public class RuminantGroup : FilterGroup<Ruminant>, IValidatableObject, IIdentifiableComponent
    {
        /// <summary>
        /// An identifier for this FilterGroup based on parent requirements
        /// </summary>
        [Description("Group identifier")]
        [Core.Display(Type = DisplayType.DropDown, Values = "ParentSuppliedIdentifiers")]
        public string Identifier { get; set; }

        /// <summary>
        /// Constructor to apply defaults
        /// </summary>
        public RuminantGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
            this.SetDefaults();
            if (!ParentSuppliedIdentifiers().Contains(Identifier))
                Identifier = "";
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

        /// <summary>
        /// A method to return the list of identifiers relavent to this ruminant group
        /// </summary>
        /// <returns>A list of identifiers as stings</returns>
        public List<string> ParentSuppliedIdentifiers()
        {
            if(Parent is CLEMRuminantActivityBase)
                return (Parent as CLEMRuminantActivityBase).DefineWorkerChildrenIdentifiers<RuminantGroup>();
            else
                return new List<string>();
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
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var identifiers = ParentSuppliedIdentifiers();
            if(identifiers.Any() & Identifier == "")
            {
                string[] memberNames = new string[] { "Ruminant group" };
                results.Add(new ValidationResult($"The group identifier [BLANK] in [f={this.Name}] is not valid for the parent activity [a={Parent.Name}].{Environment.NewLine}Select an option from the list or provide an empty value for the property if no entries are provided", memberNames));
            }
            if (identifiers.Any() & !identifiers.Contains(Identifier))
            {
                string[] memberNames = new string[] { "Ruminant group" };
                results.Add(new ValidationResult($"The group identifier [{Identifier}] in [f={this.Name}] is not valid for the parent activity [a={Parent.Name}].{Environment.NewLine}Select an option from the list or provide an empty value for the property if no entries are provided", memberNames));
            }
            return results;
        }
        #endregion

    }
}