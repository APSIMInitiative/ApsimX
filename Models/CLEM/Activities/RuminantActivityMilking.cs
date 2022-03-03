using Models.Core;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Models.Core.Attributes;
using System.IO;
using Models.CLEM.Groupings;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to undertake milking of particular herd</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Perform milking of lactating breeders")]
    [Version(1, 1, 0, "Implements event based activity control")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantMilking.htm")]
    public class RuminantActivityMilking: CLEMRuminantActivityBase, ICanHandleIdentifiableChildModels
    {
        private int numberToDo;
        private int numberToSkip;
        private double amountToDo;
        private double amountToSkip;
        private object milkStore;
        private IEnumerable<RuminantFemale> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroups;

        /// <summary>
        /// Resource type to store milk in
        /// </summary>
        [Description("Store to place milk")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(ProductStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of milk store required")]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public RuminantActivityMilking()
        {
            TransactionCategory = "Livestock.Milking";
        }

        /// <inheritdoc/>
        public override LabelsForIdentifiableChildren DefineIdentifiableChildModelLabels<T>()
        {
            switch (typeof(T).Name)
            {
                case "RuminantGroup":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>() {
                            "Individuals to milk" },
                        units: new List<string>()
                        );
                case "RuminantActivityFee":
                case "LabourRequirement":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>() {
                            "Number milked",
                            "Litres milked",
                        },
                        units: new List<string>() {
                            "fixed",
                            "per head",
                            "per L milked"
                        }
                        );
                default:
                    return new LabelsForIdentifiableChildren();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);
            filterGroups = GetIdentifiableChildrenByIdentifier<RuminantGroup>("Individuals to move", false, true);

            // find milk store
            milkStore = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, ResourceTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <summary>An event handler to call for all herd management activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalMilkProduction")]
        private void OnCLEMMilkProduction(object sender, EventArgs e)
        {
            // this method will ensure the milking status is defined for females after births when lactation is set and before milk production is determined
            foreach (RuminantFemale item in this.CurrentHerd(true).OfType<RuminantFemale>().Where(a => a.IsLactating))
                // set these females to state milking performed so they switch to the non-suckling milk production curves.
                item.MilkingPerformed = true;
        }

        /// <inheritdoc/>
        protected override List<ResourceRequest> DetermineResourcesForActivity()
        {
            amountToDo = 0;
            amountToSkip = 0;
            numberToDo = 0;
            numberToSkip = 0;
            IEnumerable<RuminantFemale> herd = GetIndividuals<RuminantFemale>(GetRuminantHerdSelectionStyle.AllOnFarm).Where(a => a.IsLactating);
            uniqueIndividuals = GetUniqueIndividuals<RuminantFemale>(filterGroups, herd);
            numberToDo = uniqueIndividuals?.Count() ?? 0;

            // provide updated units of measure for identifiable children
            foreach (var valueToSupply in valuesForIdentifiableModels.ToList())
            {
                int number = numberToDo;
                switch (valueToSupply.Key.identifier)
                {
                    case "Number milked":
                        switch (valueToSupply.Key.unit)
                        {
                            case "fixed":
                                valuesForIdentifiableModels[valueToSupply.Key] = 1;
                                break;
                            case "per head":
                                valuesForIdentifiableModels[valueToSupply.Key] = number;
                                break;
                            case "per L milked":
                                throw new NotImplementedException($"Unable to use units [{valueToSupply.Key.unit}] with [{valueToSupply.Key.identifier}] identifier in {NameWithParent}");
                            default:
                                throw new NotImplementedException($"Unknown units [{((valueToSupply.Key.unit == "") ? "Blank" : valueToSupply.Key.unit)}] for [{((valueToSupply.Key.identifier == "") ? "Blank" : valueToSupply.Key.identifier)}] identifier in [a={NameWithParent}]");
                        }
                        break;
                    case "Litres milked":
                        switch (valueToSupply.Key.unit)
                        {
                            case "fixed":
                                valuesForIdentifiableModels[valueToSupply.Key] = 1;
                                break;
                            case "per head":
                                throw new NotImplementedException($"Unable to use units [{valueToSupply.Key.unit}] with [{valueToSupply.Key.identifier}] identifier in {NameWithParent}");
                            case "per kg fleece":
                                amountToDo = uniqueIndividuals.Sum(a => a.MilkCurrentlyAvailable);
                                valuesForIdentifiableModels[valueToSupply.Key] = amountToDo;
                                break;
                            default:
                                throw new NotImplementedException($"Unknown units [{((valueToSupply.Key.unit == "") ? "Blank" : valueToSupply.Key.unit)}] for [{((valueToSupply.Key.identifier == "") ? "Blank" : valueToSupply.Key.identifier)}] identifier in [a={NameWithParent}]");
                        }
                        break;
                    default:
                        throw new NotImplementedException($"Unknown identifier [{valueToSupply.Key.identifier}] used in {NameWithParent}");
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
                var numberShort = shortfalls.Where(a => a.IdentifiableChildDetails.identifier == "Number milked").FirstOrDefault();
                if (numberShort != null)
                    numberToSkip = Convert.ToInt32(numberToDo * numberShort.Required / numberShort.Provided);

                var amountShort = shortfalls.Where(a => a.IdentifiableChildDetails.identifier == "Litres milked").FirstOrDefault();
                if (amountShort != null)
                    amountToSkip = Convert.ToInt32(amountToDo * amountShort.Required / amountShort.Provided);

                this.Status = ActivityStatus.Partial;
            }
        }

        /// <inheritdoc/>
        protected override void PerformTasksForActivity()
        {
            if (numberToDo - numberToSkip > 0)
            {
                amountToDo -= amountToSkip;
                double amountDone = 0;
                int number = 0;
                foreach (RuminantFemale ruminant in uniqueIndividuals.SkipLast(numberToSkip).ToList())
                {
                    amountDone += ruminant.MilkCurrentlyAvailable;
                    amountToDo -= ruminant.MilkCurrentlyAvailable;
                    ruminant.TakeMilk(ruminant.MilkCurrentlyAvailable, MilkUseReason.Milked);
                    number++;
                    if (amountToDo <= 0)
                        break;
                }
                // add clip to stores
                (milkStore as IResourceType).Add(amountDone, this, this.PredictedHerdName, TransactionCategory);

                if (number == numberToDo && amountToDo <= 0)
                    SetStatusSuccess();
                else
                    this.Status = ActivityStatus.Partial;
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Milk is placed in ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(ResourceTypeName, "Not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
