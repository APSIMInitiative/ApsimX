using Models.CLEM.Interfaces;
using Models.Core.Attributes;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Resources;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Models.CLEM.Groupings;
using System.Net.WebSockets;

namespace Models.CLEM.Activities
{
    /// <summary>Other animals task activity</summary>
    /// <summary>This activity allows labour and costs to be applied to selected other animals</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Allows labour and costs to be applied to specified other animals")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/OtherAnimalsActivityTask.htm")]
    public class OtherAnimalsActivityCost : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        private int numberToDo = 0;
        private IEnumerable<OtherAnimalsGroup> filterGroups;
        private OtherAnimals otherAnimals { get; set; }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "OtherAnimalsGroup":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>()
                        );
                case "LabourRequirement":
                case "ActivityFee":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Number to perform"
                        },
                        measures: new List<string>() {
                            "fixed",
                            "per head"
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            filterGroups = GetCompanionModelsByIdentifier<OtherAnimalsGroup>(false, true);
            otherAnimals = Resources.FindResourceGroup<OtherAnimals>();
        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            numberToDo = 0;

            // get all cohorts
            IEnumerable<OtherAnimalsTypeCohort> CohortsToReport = otherAnimals.GetCohorts(filterGroups, true);
            numberToDo = CohortsToReport.Sum(a => a.Number);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NotNeeded;
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.type)
                {
                    case "OtherAnimalsGroup":
                        valuesForCompanionModels[valueToSupply.Key] = numberToDo;
                        break;
                    case "LabourRequirement":
                    case "ActivityFee":
                        switch (valueToSupply.Key.identifier)
                        {
                            case "Number to buy":
                                switch (valueToSupply.Key.unit)
                                {
                                    case "fixed":
                                        valuesForCompanionModels[valueToSupply.Key] = 1;
                                        break;
                                    case "per head":
                                        valuesForCompanionModels[valueToSupply.Key] = numberToDo;
                                        break;
                                    default:
                                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                                }
                                break;
                            default:
                                throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
                        }
                        break;
                    default:
                        throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key));
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // get greatest shortfall by proportion
                var buyShort = shortfalls.OrderBy(a => a.Provided / a.Required).FirstOrDefault();
                int reduce = Convert.ToInt32(numberToDo * buyShort.Provided / buyShort.Required);
                numberToDo -= reduce;
                this.Status = ActivityStatus.Partial;
            }
            else
                this.Status = ActivityStatus.Success;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write($"Calculates the number of Other Animal individuals specified by filter groups to apply costs and labour.</br>");
                htmlWriter.Write("Each cohort will only be considered once (when first specified) regardless of multiple filter groups.");
                htmlWriter.Write("</div>");
                return htmlWriter.ToString();
            }
        }
        #endregion

    }
}
