using APSIM.Shared.Utilities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to manage external resources from resource reader
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manage the input and output of external resources specified in a file")]
    [HelpUri(@"Content/Features/Activities/All resources/ManageExternalResource.htm")]
    [Version(1, 1, 1, "Implements filtering of resources used and multiplier component")]
    public class ResourceActivityManageExternal : CLEMActivityBase, IHandlesActivityCompanionModels, IValidatableObject
    {
        [Link]
        private readonly IClock clock = null;
        private double[,] amountToDo;
        private double[,] valueToDo;
        private double[,] packetsToDo;
        private FileResource fileResource = null;
        private FinanceType bankAccount = null;
        [JsonIgnore]
        [NonSerialized]
        private DataView currentEntries;
        [JsonIgnore]
        [NonSerialized]
        private List<(IResourceType resource, double amount)> resourcesForMonth;
        IEnumerable<string> resourceTypesToInclude = null;
        Dictionary<string, (IResourceType, double)> allResources = new Dictionary<string, (IResourceType, double)>();

        /// <summary>
        /// Name of the model for the resource input file
        /// </summary>
        [Description("Name of resource data reader")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Resource data reader required")]
        [Models.Core.Display(Type = DisplayType.DropDown, Values = "GetNameOfModelsByType", ValuesArgs = new object[] { new Type[] { typeof(FileResource) } })]
        public string ResourceDataReader { get; set; }

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [System.ComponentModel.DefaultValue("No financial transactions")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "No financial implications", typeof(Finance) } })]
        public string AccountName { get; set; }

        /// <summary>
        /// Names of resource columns to consider, blank for all
        /// </summary>
        [Description("Resource types considered")]
        [Tooltip("A comma delimited of resource type names. Blank entry will include all resources")]
        public string ResourceColumnsToUse { get; set; }

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
                            "per packet incoming",
                            "per packet outgoing",
                            "per amount incoming",
                            "per amount outgoing",
                            "per dollar value incoming",
                            "per dollar value outgoing",
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
            // get bank account object to use if provided
            if (AccountName != "No financial transactions")
                bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);

            // get reader
            Model parentZone = FindAllAncestors<Zone>().FirstOrDefault();
            if(parentZone != null)
                fileResource = parentZone.FindDescendant<FileResource>(ResourceDataReader);

            resourcesForMonth = new List<(IResourceType resource, double amount)>();

            if ((ResourceColumnsToUse??"") != "")
                resourceTypesToInclude = ResourceColumnsToUse.Split(",").Select(x => x.Trim());

            // find all resources and check if related multiplier is available
            // place in dictionary for easy access during simulation
            if (fileResource != null)
            {
                foreach (var resourceName in fileResource.GetUniqueResourceTypes())
                {
                    string warn = "";
                    if (!resourceTypesToInclude.Any() || resourceTypesToInclude.Contains(resourceName))
                    {
                        IResourceType resource;
                        if (resourceName.Contains("."))
                            resource = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, resourceName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
                        else
                            resource = Resources.FindAllDescendants<IResourceType>(resourceName).FirstOrDefault();

                        switch (resource.GetType().ToString())
                        {
                            case "Models.CLEM.Resources.LandType":
                            case "Models.CLEM.Resources.RuminantType":
                            case "Models.CLEM.Resources.LabourType":
                            case "Models.CLEM.Resources.GrazeFoodStoreType":
                            case "Models.CLEM.Resources.OtherAnimalsType":
                                warn = $"[a={Name}] does not support [r={resource.GetType()}]\r\nThis resource will be ignored. Contact developers for more information";
                                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
                                resource = null;
                                break;
                            default:
                                break;
                        }

                        double resourceMultiplier = 1;

                        if (resource != null)
                        {
                            var matchingResources = FindAllChildren<ResourceActivityExternalMultiplier>().Where(a => a.ResourceTypeName == (resource as CLEMModel).NameWithParent || a.ResourceTypeName == (resource as CLEMModel).Name);
                            if (matchingResources.Count() > 1)
                            {
                                warn = $"[a={Name}] could not distinguish between multiple occurences of resource [r={resourceName}] provided by [x={fileResource.Name}] in the local [r=ResourcesHolder]\r\nEnsure all resource names are unique across stores, or use ResourceStore.ResourceType notation to specify resources in the input file";
                                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
                            }
                            resourceMultiplier = FindAllChildren<ResourceActivityExternalMultiplier>().Where(a => a.ResourceTypeName == (resource as CLEMModel).NameWithParent || a.ResourceTypeName == (resource as CLEMModel).Name).FirstOrDefault()?.Multiplier ?? 1;
                        }
                        else
                        {
                            warn = $"[a={Name}] could not find the resource [r={resourceName}] provided by [x={fileResource.Name}] in the local [r=ResourcesHolder]\r\nExternal transactions with this resource will be ignored\r\nYou can either add this resource to your simulation or remove it from the input file to avoid this warning";
                            Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
                        }

                        allResources.Add(resourceName, (resource, resourceMultiplier));
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            amountToDo = new double[,] { { 0, 0 }, { 0, 0 } };
            valueToDo = new double[,] { { 0, 0 }, { 0, 0 } };
            packetsToDo = new double[,] { { 0, 0 }, { 0, 0 } };

            // get data from reader for month
            currentEntries = fileResource.GetCurrentResourceData(clock.Today.Month, clock.Today.Year, resourceTypesToInclude, false);

            resourcesForMonth.Clear();

            if (currentEntries.Table.Rows != null)
            {
                foreach (DataRowView item in currentEntries)
                {
                    var resource = allResources[item[fileResource.ResourceNameColumnName].ToString()];
                    if(resource.Item1 != null)
                    {
                        // amount provided x any multiplier
                        double amount = Convert.ToDouble(item[fileResource.AmountColumnName], CultureInfo.InvariantCulture) * resource.Item2;

                        // get price of resource
                        ResourcePricing price = resource.Item1.Price(amount > 0 ? PurchaseOrSalePricingStyleType.Purchase : PurchaseOrSalePricingStyleType.Sale);

                        double amountAvailable = (amount < 0) ? Math.Min(Math.Abs(amount), resource.Item1.Amount) : amount;

                        double packets = amountAvailable / price.PacketSize;
                        if (price.UseWholePackets)
                            packets = Math.Truncate(packets);

                        if (MathUtilities.IsNegative(amount))
                        {
                            packetsToDo[0, 0] += packets;
                            amountToDo[0, 0] += packets * price.PacketSize;
                            valueToDo[0, 0] += packets * price.PricePerPacket;
                        }
                        else
                        {
                            packetsToDo[1, 0] += packets;
                            amountToDo[1, 0] += packets * price.PacketSize;
                            valueToDo[1, 0] += packets * price.PricePerPacket;
                        }
                        resourcesForMonth.Add((resource.Item1, amountAvailable));
                    }
                }
            }

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per amount incoming":
                        valuesForCompanionModels[valueToSupply.Key] = amountToDo[1, 0];
                        break;
                    case "per packet incoming":
                        valuesForCompanionModels[valueToSupply.Key] = packetsToDo[1, 0];
                        break;
                    case "per dollar value incoming":
                        valuesForCompanionModels[valueToSupply.Key] = valueToDo[1, 0];
                        break;
                    case "per amount outgoing":
                        valuesForCompanionModels[valueToSupply.Key] = amountToDo[0, 0];
                        break;
                    case "per packet outgoing":
                        valuesForCompanionModels[valueToSupply.Key] = packetsToDo[0, 0];
                        break;
                    case "per dollar value outgoing":
                        valuesForCompanionModels[valueToSupply.Key] = valueToDo[0, 0];
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

            // incoming or outgoing shortfalls
            int skipped = 0;
            List<string> inOut = new List<string>() { "outgoing", "incoming" };
            for (int i = 0; i < inOut.Count; i++)
            {
                var sel_shortfalls = shortfalls.Where(a => a.CompanionModelDetails.unit.Contains("incoming"));
                if (sel_shortfalls.Any())
                {
                    // purchase shortfalls
                    var unitShort = sel_shortfalls.FirstOrDefault();
                    if (OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.SkipActivity || OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.ReportErrorAndStop)
                    {
                        skipped++;
                    }
                    else
                    {
                        switch (unitShort.CompanionModelDetails.identifier.Split(" ").First())
                        {
                            case "amount":
                                amountToDo[i, 1] = Convert.ToInt32(amountToDo[i, 0] * (1 - unitShort.Available / unitShort.Required));
                                break;
                            default:
                                throw new NotImplementedException($"Cannot use {unitShort.CompanionModelDetails.identifier} unit to influence ResourceActivitymanageExternal when shortfalls occur in [a={NameWithParent}]");
                        }

                        if (unitShort.Available == 0)
                        {
                            Status = ActivityStatus.Warning;
                            AddStatusMessage($"Resource shortfall prevented any {inOut[i]} action");
                        }
                    }
                }
            }
            if (skipped == 2)
            {
                Status = ActivityStatus.Skipped;
            }
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (resourcesForMonth.Any())
            {
                double[] amountPerformed = new double[2] { 0, 0 };
                // loop through all resources to exchange and make transactions
                foreach (var resourceItem in resourcesForMonth)
                {
                    double amount = resourceItem.amount;
                    bool isSale = (amount < 0);
                    amount = Math.Abs(amount);
                    ResourcePricing price = null;
                    if (bankAccount != null && !(resourceItem.resource is FinanceType))
                        price = resourceItem.resource.Price((amount > 0 ? PurchaseOrSalePricingStyleType.Purchase : PurchaseOrSalePricingStyleType.Sale));

                    // transactions
                    if (isSale)
                    {
                        // sell, so limit to labour and amount available
                        double amountPossible = amountToDo[0, 0] - amountToDo[0, 1];
                        double amountRemaining = 0;
                        if (amountPossible > amountPerformed[0])
                            amountRemaining = amountPossible - amountPerformed[0];

                        amount = Math.Min(amountRemaining, Math.Min(amount, resourceItem.resource.Amount));
                        if (amount > 0)
                        {
                            if (price != null)
                            {
                                double packets = amount / price.PacketSize;
                                if (price.UseWholePackets)
                                {
                                    packets = Math.Truncate(packets);
                                    amount = packets * price.PacketSize;
                                }
                                bankAccount.Add(packets * price.PricePerPacket, this, (resourceItem.resource as CLEMModel).NameWithParent, "External output");
                            }
                            ResourceRequest sellRequest = new ResourceRequest
                            {
                                ActivityModel = this,
                                Required = amount,
                                AllowTransmutation = false,
                                Category = "External output",
                                RelatesToResource = (resourceItem.resource as CLEMModel).NameWithParent
                            };
                            resourceItem.resource.Remove(sellRequest);
                        }
                    }
                    else
                    {
                        double amountPossible = amountToDo[1, 0] - amountToDo[1, 1];
                        double amountRemaining = 0;
                        if (amountPossible > amountPerformed[1])
                            amountRemaining = amountPossible - amountPerformed[1];

                        amount = Math.Min(amountRemaining, amount);

                        // limit to labour and financial constraints as this is a purchase
                        if (amount > 0)
                        {
                            if (price != null)
                            {
                                // need to limit amount by financial constraints
                                double packets = amount / price.PacketSize;
                                if (price.UseWholePackets)
                                    packets = Math.Truncate(packets);

                                amount = packets * price.PacketSize;
                                ResourceRequest sellRequestDollars = new ResourceRequest
                                {
                                    ActivityModel = this,
                                    Required = packets * price.PacketSize,
                                    AllowTransmutation = false,
                                    Category = "External input",
                                    RelatesToResource = (resourceItem.resource as CLEMModel).NameWithParent
                                };
                                bankAccount.Remove(sellRequestDollars);
                            }
                            resourceItem.resource.Add(amount, this, (resourceItem.resource as CLEMModel).NameWithParent, "External input");
                        }
                    }
                }
            }
            else
            {
                if (MathUtilities.IsPositive(currentEntries.Count))
                    Status = ActivityStatus.Warning;
                return;
            }

            SetStatusSuccessOrPartial(amountToDo[0,1]+amountToDo[1,1] > 0);
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (fileResource == null)
            {
                yield return new ValidationResult("Unable to locate resource input file.\r\nAdd a [f=ResourceReader] component to the simulation tree.", new string[] { "FileResourceReader" });
            }
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">Resources added or removed are provided by ");
            htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(ResourceDataReader, "Reader not set", HTMLSummaryStyle.FileReader));
            htmlWriter.Write("</div>");
            htmlWriter.Write("\r\n<div class=\"activityentry\">");
            if (AccountName == null || AccountName == "")
                htmlWriter.Write("Financial transactions will be made to <span class=\"errorlink\">FinanceType not set</span>");
            else if (AccountName == "No financial implications")
                htmlWriter.Write("No financial constraints relating to pricing and packet sizes associated with each resource will be included.");
            else
                htmlWriter.Write("Pricing and packet sizes associated with each resource will be used with <span class=\"resourcelink\">" + AccountName + "</span>");
            htmlWriter.Write("</div>");


            htmlWriter.Write("\r\n<div class=\"activityentry\">The following resources will be included if present in the Resource File");
            htmlWriter.Write("\r\n<div class=\"filterborder clearfix\">");
            var resourceFilter = ((ResourceColumnsToUse ?? "").Length > 0) ? ResourceColumnsToUse : "All resources";
            foreach (var res in resourceFilter.Split(",").Select(x => x.Trim()))
                htmlWriter.Write($"<div class=\"filter\">{res}</div>");

            htmlWriter.Write("</div>");
            htmlWriter.Write("</div>");

            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var childList = new List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)>
            {
                (FindAllChildren<ResourceActivityExternalMultiplier>(), true, "childgroupfilterborder", "The following multipliers will be applied:", "")
            };
            return childList;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            return "";
        }

        #endregion

    }
}
