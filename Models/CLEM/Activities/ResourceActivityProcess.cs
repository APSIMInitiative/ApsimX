using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to processes one resource into another resource with associated labour and costs
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Process one resource into another resource with associated labour and costs")]
    [HelpUri(@"Content/Features/Activities/All resources/ProcessResource.htm")]
    [Version(1, 0, 1, "")]
    public class ResourceActivityProcess : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        private double amountToDo;
        private double amountToSkip;
        private ResourceRequest resourceRequest;
        [JsonIgnore]
        private IResourceType resourceTypeProcessModel { get; set; }
        [JsonIgnore]
        private IResourceType resourceTypeCreatedModel { get; set; }

        /// <summary>
        /// Resource type to process
        /// </summary>
        [Description("Resource to process")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(Equipment), typeof(GreenhouseGases), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of resource type to process required")]
        public string ResourceTypeProcessedName { get; set; }

        /// <summary>
        /// Resource type created
        /// </summary>
        [Description("Resource created")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(Equipment), typeof(GreenhouseGases), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of resource type created required")]
        public string ResourceTypeCreatedName { get; set; }

        /// <summary>
        /// Conversion rate
        /// </summary>
        [Description("Rate to convert processed resource to created resource")]
        [Required, GreaterThanValue(0)]
        public double ConversionRate { get; set; }

        /// <summary>
        /// Reserve
        /// </summary>
        [Description("Amount to reserve")]
        [Required, GreaterThanEqualValue(0)]
        public double Reserve { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            resourceTypeProcessModel = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, ResourceTypeProcessedName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            resourceTypeCreatedModel = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, ResourceTypeCreatedName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                            "per amount to process"
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            List<ResourceRequest> resourcesNeeded = new List<ResourceRequest>();
            resourceRequest = null;

            amountToSkip = 0;
            amountToDo = resourceTypeProcessModel.Amount;
            if (Reserve > 0)
            {
                amountToDo = Math.Min(amountToDo, Reserve);
            }

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per amount to process":
                        valuesForCompanionModels[valueToSupply.Key] = amountToDo;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }

            if (amountToDo > 0)
            {
                resourceRequest = new ResourceRequest()
                {
                    AllowTransmutation = false,
                    Required = amountToDo,
                    Resource = resourceTypeProcessModel,
                    ResourceType = (resourceTypeProcessModel as Model).Parent.GetType(),
                    ResourceTypeName = (resourceTypeProcessModel as Model).Name,
                    ActivityModel = this,
                    Category = TransactionCategory,
                    RelatesToResource = (resourceTypeCreatedModel as CLEMModel).NameWithParent
                };
                resourcesNeeded.Add(resourceRequest);
            }
            return resourcesNeeded;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                var unitShort = shortfalls.FirstOrDefault();
                if (OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.SkipActivity || OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.ReportErrorAndStop)
                {
                    resourceRequest.Required = 0;
                    Status = ActivityStatus.Skipped;
                }
                else
                {
                    amountToSkip = Convert.ToInt32(amountToDo * (1 - unitShort.Available / unitShort.Required));
                    resourceRequest.Required -= amountToSkip;
                    if (unitShort.Available == 0)
                    {
                        Status = ActivityStatus.Warning;
                        AddStatusMessage("Resource shortfall prevented any action");
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            // processed resource should already be taken if all was ok
            if (resourceRequest != null && resourceRequest.Provided > 0)
            {
                // add created resources
                resourceTypeCreatedModel.Add(resourceRequest.Provided * ConversionRate, this, (resourceTypeCreatedModel as CLEMModel).NameWithParent, "Created");
                SetStatusSuccessOrPartial(amountToSkip > 0);
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Process ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(ResourceTypeProcessedName, entryStyle: HTMLSummaryStyle.Resource));
                htmlWriter.Write(" into ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(ResourceTypeCreatedName, entryStyle: HTMLSummaryStyle.Resource));
                htmlWriter.Write(" at a rate of ");
                if (ConversionRate <= 0)
                    htmlWriter.Write("<span class=\"errorlink\">RATE NOT SET</span>");
                else
                    htmlWriter.Write("1:<span class=\"resourcelink\">" + ConversionRate.ToString("0.###") + "</span>");
                htmlWriter.Write("</div>");
                if (Reserve > 0)
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">{CLEMModel.DisplaySummaryValueSnippet(Reserve, warnZero:true)}");
                    htmlWriter.Write(" will be reserved.</div>");
                }
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
