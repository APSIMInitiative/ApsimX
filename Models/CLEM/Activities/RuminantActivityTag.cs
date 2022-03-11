using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Add or remove a tag to specified individual ruminants</summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Add or remove a specified tag to/from the specified individuals for customised filtering")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 2, "Uses the Attribute feature of Ruminants")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantTag.htm")]

    public class RuminantActivityTag : CLEMRuminantActivityBase, ICanHandleIdentifiableChildModels
    {
        private int numberToDo;
        private int numberToSkip;
        private IEnumerable<Ruminant> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroups;

        /// <summary>
        /// Tag label
        /// </summary>
        [Description("Label of tag")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Label for tag required")]
        public string TagLabel { get; set; }

        /// <summary>
        /// Application style - add or remove tag
        /// </summary>
        [Description("Add or remove tag")]
        [System.ComponentModel.DefaultValueAttribute(TagApplicationStyle.Add)]
        public TagApplicationStyle ApplicationStyle { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public RuminantActivityTag()
        {
            TransactionCategory = "Livestock.Manage.[Tag]";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            this.InitialiseHerd(true, true);
            filterGroups = GetIdentifiableChildrenByIdentifier<RuminantGroup>(true, false);
            // activity is performed in ManageAnimals
            this.AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForIdentifiableChildren DefineIdentifiableChildModelLabels(string type)
        {
            switch (type)
            {
                case "RuminantGroup":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>() ,
                        units: new List<string>()
                        );
                case "RuminantActivityFee":
                case "LabourRequirement":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>() {
                            "Number tagged/untagged",
                        },
                        units: new List<string>() {
                            "fixed",
                            "per head"
                        }
                        );
                default:
                    return new LabelsForIdentifiableChildren();
            }
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalMark")]
        protected override void OnGetResourcesPerformActivity(object sender, EventArgs e)
        {
            ManageActivityResourcesAndTasks();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> DetermineResourcesForActivity(double argument = 0)
        {
            numberToDo = 0;
            numberToSkip = 0;
            IEnumerable<Ruminant> herd = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.AllOnFarm).Where(a => a.Attributes.Exists(TagLabel) == (ApplicationStyle != TagApplicationStyle.Add));
            uniqueIndividuals = GetUniqueIndividuals<Ruminant>(filterGroups, herd);
            numberToDo = uniqueIndividuals?.Count() ?? 0;

            // provide updated units of measure for identifiable children
            foreach (var valueToSupply in valuesForIdentifiableModels.ToList())
            {
                int number = numberToDo;
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForIdentifiableModels[valueToSupply.Key] = 1;
                        break;
                    case "per head":
                        valuesForIdentifiableModels[valueToSupply.Key] = number;
                        break;
                    default:
                        if(valueToSupply.Key.type != "RuminantGroup")
                        {
                            throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                        }
                        break;
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForActivity()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var tagsShort = shortfalls.Where(a => a.IdentifiableChildDetails.identifier == "Number tagged/untagged").FirstOrDefault();
                if (tagsShort != null)
                    numberToSkip = Convert.ToInt32(numberToDo * tagsShort.Required / tagsShort.Provided);

                this.Status = ActivityStatus.Partial;
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForActivity(double argument = 0)
        {
            if(numberToDo - numberToSkip > 0)
            {
                int tagged = 0;
                foreach (Ruminant ruminant in uniqueIndividuals.SkipLast(numberToSkip).ToList())
                {
                    switch (ApplicationStyle)
                    {
                        case TagApplicationStyle.Add:
                            ruminant.Attributes.Add(TagLabel);
                            break;
                        case TagApplicationStyle.Remove:
                            ruminant.Attributes.Add(TagLabel);
                            break;
                    }
                    tagged++;
                }
                if (tagged == numberToDo)
                    SetStatusSuccessOrPartial();
                else
                    this.Status = ActivityStatus.Partial;
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string tagstring = CLEMModel.DisplaySummaryValueSnippet(TagLabel, "Not set");
            return $"\r\n<div class=\"activityentry\">{ApplicationStyle} the tag {tagstring} {((ApplicationStyle == TagApplicationStyle.Add)?"to":"from")} all individuals in the following groups</div>";
        }
        #endregion
    }
}
