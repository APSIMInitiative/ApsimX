using Models.Core;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using Models.Core.Attributes;

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
        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityTask()
        {
            this.SetDefaults();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            List<ResourceRequest> resourcesNeeded = null;
            return resourcesNeeded;
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

    }
}
