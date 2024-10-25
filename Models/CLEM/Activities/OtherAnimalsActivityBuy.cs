using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.PMF.Organs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to purchase other animals
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manages the purchase of specified other animals")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/BuyOtherAnimals.htm")]
    public class OtherAnimalsActivityBuy : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        IEnumerable<OtherAnimalsTypeCohort> cohorts = null;
        private int numberToBuy = 0;
        private double purchaseValue = 0;

        /// <summary>
        /// The name of the animal type.
        /// </summary>
        [JsonIgnore]
        public string PredictedAnimalType { get; set; } = "";

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            cohorts = FindAllChildren<OtherAnimalsTypeCohort>();
            foreach (var cohort in cohorts)
            {
                cohort.AdjustedNumber = cohort.Number;
            }

            var animalTypesPresent = cohorts.Select(a => a.AnimalTypeName.Split('.')[1]).Distinct();
            if (!animalTypesPresent.Any())
                return;

            if (animalTypesPresent.Count() == 1)
                PredictedAnimalType = string.Join(',', animalTypesPresent);
            else
                PredictedAnimalType = "Mixed types";
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "ActivityFee":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Number to buy",
                            "Purchase value"
                        },
                        measures: new List<string>() {
                            "fixed",
                            "per head",
                            "value of individuals"
                        }
                        );
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Number purchased"
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

        /// <summary>
        /// Constructor
        /// </summary>
        public OtherAnimalsActivityBuy()
        {
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            foreach (var cohort in cohorts)
                cohort.AdjustedNumber = cohort.Number;
            numberToBuy = cohorts.Sum(a => a.Number);
            purchaseValue = cohorts.Sum(a => a.Number * a.AnimalType.GetPriceGroupOfCohort(a, PurchaseOrSalePricingStyleType.Sale)?.Value ?? 0);
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
                        valuesForCompanionModels[valueToSupply.Key] = numberToBuy;
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
                                        valuesForCompanionModels[valueToSupply.Key] = numberToBuy;
                                        break;
                                    default:
                                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                                }
                                break;
                            case "Purchase value":
                                switch (valueToSupply.Key.unit)
                                {
                                    case "fixed":
                                        valuesForCompanionModels[valueToSupply.Key] = purchaseValue;
                                        break;
                                    case "per head":
                                        // calculate value
                                        valuesForCompanionModels[valueToSupply.Key] = purchaseValue/numberToBuy;
                                        break;
                                    case "value of individuals":
                                        // calculate value
                                        valuesForCompanionModels[valueToSupply.Key] = purchaseValue;
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

                foreach (var cohort in cohorts.Where(a => a.Number > 0))
                {
                    int reduce = Convert.ToInt32((cohort.Number - cohort.AdjustedNumber) * buyShort.Provided / buyShort.Required);
                    cohort.AdjustedNumber -= reduce;
                    this.Status = ActivityStatus.Partial;
                }
            }
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalBuy")]
        private void OnCLEMAnimalBuyPerformActivity(object sender, EventArgs e)
        {
            if (TimingOK)
            {
                ManageActivityResourcesAndTasks();

                int numberAdjusted = cohorts.Sum(a => a.AdjustedNumber);
                foreach (var cohort in cohorts.Where(a => a.Number > 0))
                {
                    cohort.AnimalType.Add(cohort, this, null, "Purchase");
                }
                if(numberAdjusted == numberToBuy)
                    Status = ActivityStatus.Success;
                else
                    Status = ActivityStatus.Partial;
            }
        }
    }
}
