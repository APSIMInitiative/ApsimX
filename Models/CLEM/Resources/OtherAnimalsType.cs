using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.PMF.Organs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for bank account
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(OtherAnimals))]
    [Description("This resource represents an other animal type (e.g. chickens)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Other animals/OtherAnimalType.htm")]
    public class OtherAnimalsType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType, IHandlesActivityCompanionModels
    {
        private List<AnimalPriceGroup> priceGroups = new List<AnimalPriceGroup>();
        private int nextCohortIndex = 1;

        /// <summary>
        /// Age (months) to weight relationship
        /// </summary>
        [XmlIgnore]
        public Relationship AgeWeightRelationship { get; set; } = null;

        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; set; }

        /// <summary>
        /// Age when individuals die
        /// </summary>
        [Description("Maximum age before death (months)")]
        [Required, GreaterThanValue(0.0)]
        public double MaxAge { get; set; }

        /// <summary>
        /// Current cohorts of this Other Animal Type.
        /// </summary>
        private List<OtherAnimalsTypeCohort> Cohorts;

        /// <summary>
        /// Current value of individuals in the herd
        /// </summary>
        [JsonIgnore]
        public AnimalPricing PriceList;

        /// <summary>
        /// Determine if a price schedule has been provided for this breed
        /// </summary>
        /// <returns>boolean</returns>
        public bool PricingAvailable() { return (PriceList != null); }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            // locate age to weight relationship
            AgeWeightRelationship = this.FindAllChildren<Relationship>().FirstOrDefault(a => a.Identifier == "Age to weight");

            PriceList = this.FindAllChildren<AnimalPricing>().FirstOrDefault();
            // Components are not permanently modifed during simulation so no need for clone: PriceList = Apsim.Clone(this.FindAllChildren<AnimalPricing>().FirstOrDefault()) as AnimalPricing;
            priceGroups = PriceList.FindAllChildren<AnimalPriceGroup>().Cast<AnimalPriceGroup>().ToList();

            Initialise();
        }

        /// <summary>
        /// Method to return the selected cohorts based on filtering by multiple cohort groups
        /// </summary>
        /// <returns>An IEnumberable list of selected cohorts.</returns>
        public IEnumerable<OtherAnimalsTypeCohort> GetCohorts(IEnumerable<OtherAnimalsGroup> filtergroups, bool includeTakeFilters)
        {
            if (filtergroups == null || filtergroups.Any() == false)
            {
                foreach (var cohort in Cohorts)
                    yield return cohort;
                yield break;
            }

            // clear considered flags
            bool multipleFilterGroups = filtergroups.Count() > 1;
            if (multipleFilterGroups)
                ClearCohortConsideredFlags();

            foreach (var filter in filtergroups)
            {
                var filteredCohorts = GetCohorts(filter, includeTakeFilters, multipleFilterGroups);
                foreach (var cohort in filteredCohorts)
                    yield return cohort;
            }
        }

        /// <summary>
        /// Method to return the selected cohorts based on a single filter group
        /// </summary>
        /// <param name="filter">An OtherAnimalsGroup to apply as filter</param>
        /// <param name="includeTakeFilters">Switch to specifiy if TakeFilters are to be mannaged</param>
        /// <param name="applyPreviouslyConsidered">Switch to specifiy if the considered state of cohort is used</param>
        /// <returns>An IEnumerable list of available cohorts</returns>
        public IEnumerable<OtherAnimalsTypeCohort> GetCohorts(OtherAnimalsGroup filter, bool includeTakeFilters, bool applyPreviouslyConsidered = false)
        {
            var filteredCohorts = filter.Filter(Cohorts.Where(a => a.Number > 0));

            if (includeTakeFilters && filteredCohorts.Any())
            {
                ApplyTakeFilters(filteredCohorts.Where(a => (applyPreviouslyConsidered ? (a.Considered == false) : true)), filter);
            }

            foreach (var cohort in filteredCohorts)
            {
                if (cohort.Considered == false)
                {
                    cohort.Considered = true;
                    yield return cohort;
                }
            }
        }

        private static void ApplyTakeFilters(IEnumerable<OtherAnimalsTypeCohort> filteredCohorts, OtherAnimalsGroup filter)
        {
            if(!filter.FindAllChildren<TakeFromFiltered>().Any())
            {
                return;
            }

            // adjust the numbers based on take and skip filters
            IEnumerable<TakeFromFiltered> takeFilters = filter.FindAllChildren<TakeFromFiltered>();
            foreach (var takeFilter in takeFilters)
            {
                int totalNumber = filteredCohorts.Sum(a => a.AdjustedNumber);
                int numberToTake = 0;
                int numberToSkip = 0;

                switch (takeFilter.TakeStyle)
                {
                    case TakeFromFilterStyle.TakeProportion:
                        numberToTake = Convert.ToInt32(totalNumber * takeFilter.Value);
                        break;
                    case TakeFromFilterStyle.TakeIndividuals:
                        numberToTake = Convert.ToInt32(takeFilter.Value);
                        break;
                    case TakeFromFilterStyle.SkipProportion:
                        numberToSkip = Convert.ToInt32(totalNumber * takeFilter.Value);
                        numberToTake = totalNumber - numberToSkip;
                        break;
                    case TakeFromFilterStyle.SkipIndividuals:
                        numberToSkip = Convert.ToInt32(takeFilter.Value);
                        numberToTake = totalNumber - numberToSkip;
                        break;
                    default:
                        break;
                }

                if (numberToSkip == 0 & totalNumber - numberToTake > 0 & takeFilter.TakePositionStyle == TakeFromFilteredPositionStyle.End)
                {
                    numberToSkip = totalNumber - numberToTake;
                }

                // step through cohorts and adjust numbers based on skip and take using position start/end
                foreach (OtherAnimalsTypeCohort cohort in filteredCohorts)
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

        /// <summary>
        /// Provides the next unique cohort index
        /// </summary>
        /// <returns>Cohort id</returns>
        private int GetNextCohortIndex()
        {
            int index = nextCohortIndex;
            nextCohortIndex++;
            return index;
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            Cohorts?.Clear();
            Cohorts = null;
        }

        /// <summary>
        /// Time step clean up
        /// </summary>
        [EventSubscribe("CLEMEndOfTimeStep")]
        private void OnEndOfTimneStep(object sender, EventArgs e)
        {
            Cohorts.RemoveAll(cohort => cohort.Number == 0);
        }

        /// <summary>
        /// Initialise resource type
        /// </summary>
        public void Initialise()
        {
            Cohorts = new List<OtherAnimalsTypeCohort>();
            foreach (var child in this.Children)
            {
                if (child is OtherAnimalsTypeCohort cohort)
                {
                    cohort.SaleFlag = HerdChangeReason.InitialHerd;
                    cohort.AdjustedNumber = cohort.Number;
                    Add(child, null, null, "Initial numbers");
                }
            }
        }

        /// <summary>
        /// Reset all AlreadyConsidered flags
        /// </summary>
        private void ClearCohortConsideredFlags()
        {
            foreach (var cohort in Cohorts)
            {
                cohort.Considered = false;
            }
        }

        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                // ToDo: need to implement this using the price list
                return 0;
            }
        }

        /// <inheritdoc/>
        public LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "Relationship":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() { "Age to weight" },
                        measures: new List<string>()
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>
        /// Get value of a specific individual
        /// </summary>
        /// <returns>value</returns>
        public AnimalPriceGroup GetPriceGroupOfCohort(OtherAnimalsTypeCohort cohort, PurchaseOrSalePricingStyleType purchaseStyle, string warningMessage = "")
        {
            if (PricingAvailable())
            {
                AnimalPriceGroup animalPrice = (purchaseStyle == PurchaseOrSalePricingStyleType.Purchase) ? cohort.CurrentPriceGroups.Buy : cohort.CurrentPriceGroups.Sell;
                if (animalPrice == null || !animalPrice.Filter(cohort))
                {
                    // search through RuminantPriceGroups for first match with desired purchase or sale flag
                    foreach (AnimalPriceGroup priceGroup in priceGroups.Where(a => a.PurchaseOrSale == purchaseStyle || a.PurchaseOrSale == PurchaseOrSalePricingStyleType.Both))
                        if (priceGroup.Filter(cohort))
                        {
                            if (purchaseStyle == PurchaseOrSalePricingStyleType.Purchase)
                            {
                                cohort.CurrentPriceGroups = (priceGroup, cohort.CurrentPriceGroups.Sell);
                                return priceGroup;
                            }
                            else
                            {
                                cohort.CurrentPriceGroups = (cohort.CurrentPriceGroups.Buy, priceGroup);
                                return priceGroup;
                            }
                        }

                    // no price match found.
                    string warningString = warningMessage;
                    if (warningString == "")
                        warningString = $"No [{purchaseStyle}] price entry was found for [r={cohort.Name}] meeting the required criteria [f=age: {cohort.Age}] [f=sex: {cohort.Sex}] [f=weight: {cohort.Weight:##0}]";
                    Warnings.CheckAndWrite(warningString, Summary, this, MessageType.Warning);
                }
                return animalPrice;
            }
            return null;
        }


        #region Transactions

        /// <summary>
        /// Amount
        /// </summary>
        [JsonIgnore]
        public double Amount { get; set; }

        /// <summary>
        /// Add individuals to type based on cohort
        /// </summary>
        /// <param name="addIndividuals">OtherAnimalsTypeCohort Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object addIndividuals, CLEMModel activity, string relatesToResource, string category)
        {
            if (addIndividuals is OtherAnimalsTypeCohort cohortDetails && cohortDetails.Number > 0)
            {
                OtherAnimalsTypeCohort cohortToAdd = null;
                OtherAnimalsTypeCohort cohortexists = Cohorts.Where(a => a.Age == cohortDetails.Age && a.Sex == cohortDetails.Sex).FirstOrDefault();

                if (cohortexists == null)
                {
                    cohortToAdd = (cohortDetails).Clone() as OtherAnimalsTypeCohort;
                    cohortToAdd.Number = cohortToAdd.AdjustedNumber;
                    cohortToAdd.AnimalType = this;
                    cohortToAdd.AnimalTypeName = this.NameWithParent;
                    cohortToAdd.ID = GetNextCohortIndex();
                    Cohorts.Add(cohortToAdd);
                }
                else
                {
                    cohortexists.Number += cohortDetails.Number;
                }
                (Parent as OtherAnimals).LastCohortChanged = (cohortexists != null)?cohortexists:cohortToAdd;
                ReportTransaction(TransactionType.Gain, cohortToAdd.Number, activity, relatesToResource, category, this, ((cohortexists != null) ? cohortexists : cohortToAdd));
            }
        }

        /// <summary>
        /// Remove individuals from type based on cohort
        /// </summary>
        /// <param name="removeIndividuals"></param>
        /// <param name="activity"></param>
        /// <param name="reason"></param>
        public void Remove(object removeIndividuals, CLEMModel activity, string reason)
        {
            if (removeIndividuals is OtherAnimalsTypeCohort cohortDetails && cohortDetails.Number > 0)
            {
                OtherAnimalsTypeCohort cohortexists = Cohorts.Where(a => a.Age == cohortDetails.Age && a.Sex == cohortDetails.Sex).FirstOrDefault();

                if (cohortexists == null)
                {
                    // tried to remove individuals that do not exist
                    throw new Exception($"Tried to remove individuals from [r={this.Name}] that do not exist [Sex: {cohortDetails.Sex}, Age: {cohortDetails.Age}]");
                }

                cohortexists.Number = Math.Max(0, cohortexists.Number - cohortDetails.AdjustedNumber);

                (Parent as OtherAnimals).LastCohortChanged = cohortexists;
                ReportTransaction(TransactionType.Loss, cohortDetails.AdjustedNumber, activity, "", reason, this, cohortexists);
            }
        }

        /// <summary>
        /// Set the amount in an account.
        /// </summary>
        /// <param name="newAmount"></param>
        public new void Set(double newAmount)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
