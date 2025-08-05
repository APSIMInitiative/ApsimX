using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Provides a link to an existing labour group to identify individual ruminants
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourRequirement))]
    [ValidParent(ParentType = typeof(LabourRequirementNoUnitSize))]
    [ValidParent(ParentType = typeof(LabourGroup))]
    [ValidParent(ParentType = typeof(TransmuteLabour))]
    [Description("This filter group provides a link to an existing labour group")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/LabourGroupLinked.htm")]
    public class LabourGroupLinked : LabourGroup, IValidatableObject
    {
        [NonSerialized]
        private IEnumerable<LabourGroup> groupsAvailable;
        [NonSerialized]
        private LabourGroup linkedGroup;

        /// <summary>
        /// Linked existing timer
        /// </summary>
        [Description("Existing labour group to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetAllLabourGroupNames")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "An existing group must be selected")]
        public string ExistingGroupName { get; set; }

        private void GetAllLabourGroupsAvailable()
        {
            var zone = FindAncestor<Zone>();
            groupsAvailable = Structure.FindChildren<LabourGroup>(relativeTo: zone, recurse: true).Where(a => a.Enabled);
        }

        private List<string> GetAllLabourGroupNames()
        {
            GetAllLabourGroupsAvailable();
            return groupsAvailable.Cast<Model>().Select(a => $"{a.Parent.Name}.{a.Name}").ToList();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            GetAllLabourGroupsAvailable();
            linkedGroup = groupsAvailable.Cast<Model>().Where(a => $"{a.Parent.Name}.{a.Name}" == ExistingGroupName).FirstOrDefault() as LabourGroup;
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
                htmlWriter.Write($"<div class=\"filtername\">{Name} is linked to </div>");

                var zone = FindAncestor<Zone>();
                var foundGroup = Structure.FindChildren<LabourGroup>(relativeTo: zone, recurse: true).Where(a => a.Enabled).Cast<Model>().Where(a => $"{a.Parent.Name}.{a.Name}" == ExistingGroupName).FirstOrDefault() as LabourGroup;
                if (foundGroup != null)
                    htmlWriter.Write(foundGroup.GetFullSummary(foundGroup, new List<string>(), ""));
                else
                {
                    if ((ExistingGroupName ?? "") == "")
                        htmlWriter.Write("<div class=\"errorbanner\">Linked LabourGroup not specified</div>");
                    else
                        htmlWriter.Write($"<div class=\"errorbanner\">Linked LabourGroup <span class=\"setvalue\">{ExistingGroupName}</span> not found</div>");
                }
                return htmlWriter.ToString();
            }
        }

        #endregion

    }
}
