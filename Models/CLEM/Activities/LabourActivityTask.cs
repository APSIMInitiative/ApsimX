using Models.Core;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using Models.Core.Attributes;
using System.Linq;

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
    public class LabourActivityTask : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        private double amountToSkip = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityTask()
        {
            this.SetDefaults();
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed"
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            amountToSkip = 0;

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
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
                // find shortfall by identifiers as these may have different influence on outcome
                var tagsShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == "").FirstOrDefault();
                //if (tagsShort != null)
                amountToSkip = (1 - tagsShort.Available / tagsShort.Required);
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (ResourceRequestList.Any())
            {
                SetStatusSuccessOrPartial(amountToSkip > 0);
            }
        }


    }
}
