using Models.Core.Attributes;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Interfaces;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Microsoft.VisualBasic.FileIO;
using System.Text.Json.Serialization;
using Models.LifeCycle;
using Models.PMF.Organs;
using Microsoft.CodeAnalysis.CSharp;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to price and sell other animals
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manages the sale of specified other animals")]
    [HelpUri(@"Content/Features/Activities/OtherAnimals/SellOtherAnimals.htm")]
    public class OtherAnimalsActivitySell : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        private IEnumerable<OtherAnimalsGroup> filterGroups;
        private int numberToDo = 0;
        private int numberSold = 0;
        private double totalValue = 0;
        private FinanceType bankAccount = null;

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "No finance required", typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of account to use required")]
        [System.ComponentModel.DefaultValueAttribute("No finance required")]
        public string BankAccountName { get; set; }

        /// <summary>
        /// Sale flag to use
        /// </summary>
        [Description("Sale reason to apply")]
        [GreaterThanValue(0, ErrorMessage = "A sale reason must be provided")]
        [HerdSaleReason("sale", ErrorMessage = "The herd change reason provided must relate to a sale")]
        public HerdChangeReason SaleFlagToUse { get; set; } = HerdChangeReason.MarkedSale;

        /// <summary>
        /// The name of the animal type.
        /// </summary>
        [JsonIgnore]
        public string PredictedAnimalType { get; set; } = "";

        /// <summary>
        /// The list of cohorts remaining to be sold in the current time-step
        /// </summary>
        [JsonIgnore]
        public IEnumerable<OtherAnimalsTypeCohort> CohortsToBeSold { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public OtherAnimalsActivitySell()
        {
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            filterGroups = GetCompanionModelsByIdentifier<OtherAnimalsGroup>(true, false);

            // check if finance is available and warn if not supplying bank account.
            if (Resources.ResourceItemsExist<Finance>())
            {
                if (BankAccountName == "")
                    Summary.WriteMessage(this, $"No bank account has been specified in [a={this.Name}] while Finances are available in the simulation. No financial transactions will be recorded for the purchase and sale of animals.", MessageType.Warning);
            }
            if (BankAccountName != "")
                bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

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
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Number to sell",
                            "Value of sales"
                        },
                        measures: new List<string>() {
                            "fixed",
                            "per head",
                            "total"
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalSell")]
        private void OnCLEMAnimalBuyPerformActivity(object sender, EventArgs e)
        {
            if (TimingOK)
            {
                ManageActivityResourcesAndTasks();
            }
        }

        /// <inheritdoc/>
        public override void PrepareForTimestep()
        {
            List<OtherAnimalsType> predictedTypes = new ();

            foreach (var filter in filterGroups)
            {
                foreach (OtherAnimalsTypeCohort cohort in filter.SelectedOtherAnimalsType.Cohorts)
                {
                    cohort.AdjustedNumber = cohort.Number;
                    if(!predictedTypes.Contains(cohort.AnimalType))
                    {
                        predictedTypes.Add(cohort.AnimalType);
                    }
                }
            }

            if (predictedTypes.Count == 0)
                return;

            if (predictedTypes.Count == 1)
                PredictedAnimalType = string.Join(',', predictedTypes.Select(a => a.Name));
            else
                PredictedAnimalType = "Mixed types";

            CohortsToBeSold = new HashSet<OtherAnimalsTypeCohort>();

            // reset all alreadyconsidered flags
            foreach (var oaType in predictedTypes)
            {
                oaType.ClearCohortConsideredFlags();
            }

            foreach (var filter in filterGroups)
            {
                IEnumerable<OtherAnimalsTypeCohort> cohorts = filter.Filter(filter.SelectedOtherAnimalsType.Cohorts.Where(a => a.Considered == false));

                IEnumerable<TakeFromFiltered> takeSkipFilters = filter.FindAllChildren<TakeFromFiltered>();

                if (cohorts.Any() && takeSkipFilters.Any())
                {
                    // adjust the numbers based on take and skip filters
                    foreach (var child in takeSkipFilters)
                    {
                        int totalNumber = cohorts.Sum(a => a.AdjustedNumber);
                        int numberToTake = 0;
                        int numberToSkip = 0;

                        switch (child.TakeStyle)
                        {
                            case TakeFromFilterStyle.TakeProportion:
                                numberToTake = Convert.ToInt32(totalNumber * child.Value);
                                break;
                            case TakeFromFilterStyle.TakeIndividuals:
                                numberToTake = Convert.ToInt32(child.Value);
                                break;
                            case TakeFromFilterStyle.SkipProportion:
                                numberToSkip = Convert.ToInt32(totalNumber * child.Value);
                                numberToTake = totalNumber - numberToSkip;
                                break;
                            case TakeFromFilterStyle.SkipIndividuals:
                                numberToSkip = Convert.ToInt32(child.Value);
                                numberToTake = totalNumber - numberToSkip;
                                break;
                            default:
                                break;
                        }

                        if (numberToSkip == 0 & totalNumber - numberToTake > 0 & child.TakePositionStyle == TakeFromFilteredPositionStyle.End)
                        {
                            numberToSkip = totalNumber - numberToTake;
                        }

                        // step through cohorts and adjust numbers based on skip and take using position start/end
                        foreach (OtherAnimalsTypeCohort cohort in cohorts)
                        {
                            if (numberToSkip > 0)
                            {
                                int numberSkipped = Math.Min(numberToSkip, cohort.AdjustedNumber);
                                numberToSkip -= numberSkipped;
                                cohort.AdjustedNumber -= numberSkipped;
                            }
                            if (cohort.AdjustedNumber > 0 & numberToTake > 0)
                            {
                                int numberTaken = Math.Min(numberToTake, cohort.AdjustedNumber);
                                numberToTake -= numberTaken;
                                cohort.AdjustedNumber = numberTaken;
                            }
                        }
                    }
                }
                foreach (var cohort in cohorts)
                {
                    cohort.Considered = true;
                }
                CohortsToBeSold = CohortsToBeSold.Union(cohorts);
            }

            // number to sell
            numberToDo = CohortsToBeSold.Sum(a => a.AdjustedNumber);
            numberSold = 0;
            // value of sale
            totalValue = CohortsToBeSold.Sum(a => a.AdjustedNumber * a.AnimalType.GetPriceGroupOfCohort(a, PurchaseOrSalePricingStyleType.Sale)?.Value??0);
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
                            case "Number to sell":
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
                            case "Value of sales":
                                switch (valueToSupply.Key.unit)
                                {
                                    case "fixed":
                                        valuesForCompanionModels[valueToSupply.Key] = 1;
                                        break;
                                    case "total":
                                        valuesForCompanionModels[valueToSupply.Key] = totalValue;
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
                var sellShort = shortfalls.OrderBy(a => a.Provided / a.Required).FirstOrDefault();

                foreach (var cohort in CohortsToBeSold.Where(a => a.AdjustedNumber > 0 & a.Number > 0))
                {
                    int reduce = Convert.ToInt32((cohort.Number - cohort.AdjustedNumber) * sellShort.Provided / sellShort.Required);
                    cohort.AdjustedNumber -= reduce;
                }
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            Status = ActivityStatus.NotNeeded;
            // Perform sales

            double finalSaleAmount = 0;
            // walk through each OtherAnimalsGroup and sell the required number using the NumberAdjusted property
            foreach (var cohort in CohortsToBeSold.Where(a => a.AdjustedNumber > 0))
            {
                OtherAnimalsTypeCohort newCohort = new ()
                {
                    Age = cohort.Age,
                    Weight = cohort.Weight,
                    Number = cohort.AdjustedNumber,
                    Sex = cohort.Sex,
                    SaleFlag = SaleFlagToUse,
                    AnimalType = cohort.AnimalType,
                    AnimalTypeName = cohort.AnimalTypeName
                };
                numberSold += newCohort.Number;
                finalSaleAmount += newCohort.Number * cohort.CurrentPriceGroups.Sell.Value;
                cohort.AnimalType.Remove(newCohort, this, SaleFlagToUse.ToString());
            }

            if (numberSold == 0)
                return;

            // payment to nominated bank accounts
            bankAccount?.Add(finalSaleAmount, this, PredictedAnimalType, TransactionCategory);

            SetStatusSuccessOrPartial(numberSold < numberToDo);
        }
    }
}
