using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

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
    [HelpUri(@"Content/Features/Activities/OtherAnimals/OtherAnimalsActivityBuy.htm")]
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
            cohorts = Structure.FindChildren<OtherAnimalsTypeCohort>();
            foreach (var cohort in cohorts)
            {
                cohort.AdjustedNumber = cohort.Number;
            }

            var animalTypesPresent = cohorts.Select(a => a.AnimalTypeName.Split('.')[1]).Distinct();
            if (!animalTypesPresent.Any())
            {
                return;
            }

            if (animalTypesPresent.Count() == 1)
            {
                PredictedAnimalType = string.Join(',', animalTypesPresent);
            }
            else
            {
                PredictedAnimalType = "Mixed types";
            }
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            return type switch
            {
                "ActivityFee" => new LabelsForCompanionModels(
                                        identifiers: [
                                            "Number to buy",
                                            "Purchase value"
                                        ],
                                        measures: [
                                            "fixed",
                                            "per head",
                                            "value of individuals"
                                        ]
                                        ),
                "LabourRequirement" => new LabelsForCompanionModels(
                                        identifiers: [
                                            "Number purchased"
                                        ],
                                        measures: [
                                            "fixed",
                                            "per head"
                                        ]
                                        ),
                _ => new LabelsForCompanionModels(),
            };
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
            {
                cohort.AdjustedNumber = cohort.Number;
            }

            numberToBuy = cohorts.Sum(a => a.Number);
            purchaseValue = cohorts.Sum(a => a.Number * a.AnimalType.GetPriceGroupOfCohort(a, PurchaseOrSalePricingStyleType.Sale)?.Value ?? 0);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NotNeeded;
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                valuesForCompanionModels[valueToSupply.Key] = valueToSupply.Key.type switch
                {
                    "OtherAnimalsGroup" => numberToBuy,
                    "LabourRequirement" or "ActivityFee" => valueToSupply.Key.identifier switch
                    {
                        "Number to buy" => valueToSupply.Key.unit switch
                        {
                            "fixed" => 1,
                            "per head" => numberToBuy,
                            _ => throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key)),
                        },
                        "Purchase value" => valueToSupply.Key.unit switch
                        {
                            "fixed" => purchaseValue,
                            "per head" => purchaseValue / numberToBuy,// calculate value
                            "value of individuals" => (double?)purchaseValue,// calculate value
                            _ => throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key)),
                        },
                        _ => throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key)),
                    },
                    _ => throw new NotImplementedException(UnknownCompanionModelErrorText(this, valueToSupply.Key)),
                };
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
                    cohort.AnimalType.AddToResource(cohort, this, null, "Purchase");
                }
                if (numberAdjusted == numberToBuy)
                {
                    Status = ActivityStatus.Success;
                }
                else
                {
                    Status = ActivityStatus.Partial;
                }
            }
        }
    }
}
