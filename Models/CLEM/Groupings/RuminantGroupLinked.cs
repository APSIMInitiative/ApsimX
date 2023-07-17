using Models.CLEM.Activities;
using Models.CLEM.Reporting;
using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Provides a link to an existing ruminant group to identify individual ruminants
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
    [Description("This filter group provides a link to an existing ruminant group")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantGroupLinked.htm")]
    public class RuminantGroupLinked : RuminantGroup, IValidatableObject
    {
        [NonSerialized]
        private IEnumerable<RuminantGroup> groupsAvailable;
        [NonSerialized]
        private RuminantGroup linkedGroup;

        /// <summary>
        /// Linked existing timer
        /// </summary>
        [Description("Existing ruminant group to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetAllRuminantGroupNames")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "An existing group must be selected")]
        public string ExistingGroupName { get; set; }

        private void GetAllRuminantGroupsAvailable()
        {
            groupsAvailable = FindAncestor<Zone>().FindAllDescendants<RuminantGroup>().Where(a => a.Enabled);
        }

        private List<string> GetAllRuminantGroupNames()
        {
            GetAllRuminantGroupsAvailable();
            return groupsAvailable.Cast<Model>().Select(a => $"{a.Parent.Name}.{a.Name}").ToList();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            GetAllRuminantGroupsAvailable();
            linkedGroup = groupsAvailable.Cast<Model>().Where(a => $"{a.Parent.Name}.{a.Name}" == ExistingGroupName).FirstOrDefault() as RuminantGroup;
        }

        /// <inheritdoc/>
        public override IEnumerable<T> Filter<T>(IEnumerable<T> source)
        {
            return linkedGroup.Filter<T>(source);
        }

        ///<inheritdoc/>
        public override bool Filter<T>(T item)
        {
            return linkedGroup.Filter<T>(item);
        }

        #region validation

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (linkedGroup is null)
            {
                string[] memberNames = new string[] { "Linked filter group" };
                string errorMsg = string.Empty;
                if (ExistingGroupName is null)
                    errorMsg = "No existing filter group has been specified";
                else
                    errorMsg = $"The filter group [f={ExistingGroupName}] could not be found.{Environment.NewLine}Ensure the name matches the name of an enabled group in the simulation tree below the same ZoneCLEM";

                results.Add(new ValidationResult(errorMsg, memberNames));
            }
            return results;
        }
        #endregion

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
        public override string ModelSummaryInnerOpeningTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                //if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                htmlWriter.Write($"{Name}");
                if ((Identifier ?? "") != "")
                    htmlWriter.Write($" - applies to {Identifier} and");
                htmlWriter.Write($" linked to </div>");

                var foundGroup = FindAncestor<Zone>().FindAllDescendants<RuminantGroup>().Where(a => a.Enabled).Cast<Model>().Where(a => $"{a.Parent.Name}.{a.Name}" == ExistingGroupName).FirstOrDefault() as RuminantGroup;
                if (foundGroup != null)
                    htmlWriter.Write(foundGroup.GetFullSummary(foundGroup, new List<string>(), ""));
                else
                {
                    if ((ExistingGroupName ?? "") == "")
                        htmlWriter.Write("<div class=\"errorbanner\">Linked RuminantGroup not specified</div>");
                    else
                        htmlWriter.Write($"<div class=\"errorbanner\">Linked RuminantGroup <span class=\"setvalue\">{ExistingGroupName}</span> not found</div>");
                }
                return htmlWriter.ToString();
            }
        }

        #endregion
    }
}
