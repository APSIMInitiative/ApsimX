using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
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
        [JsonIgnore]
        public List<OtherAnimalsTypeCohort> Cohorts;

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

            // clone pricelist so model can modify if needed and not affect initial parameterisation
            if (FindAllChildren<AnimalPricing>().Count() > 0)
            {
                PriceList = this.FindAllChildren<AnimalPricing>().FirstOrDefault();
                // Components are not permanently modifed during simulation so no need for clone: PriceList = Apsim.Clone(this.FindAllChildren<AnimalPricing>().FirstOrDefault()) as AnimalPricing;

                priceGroups = PriceList.FindAllChildren<AnimalPriceGroup>().Cast<AnimalPriceGroup>().ToList();
            }
            Initialise();
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
                    Add(child, null, null, "Initial numbers");
                }
            }
        }

        /// <summary>
        /// Reset all AlreadyConsidered flags
        /// </summary>
        public void ClearCohortConsideredFlags()
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
                return Price(PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(Amount);
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
            OtherAnimalsTypeCohort cohortToAdd = (OtherAnimalsTypeCohort)(addIndividuals as OtherAnimalsTypeCohort).Clone();

            OtherAnimalsTypeCohort cohortexists = Cohorts.Where(a => a.Age == cohortToAdd.Age && a.Sex == cohortToAdd.Sex).FirstOrDefault();

            if (cohortexists == null)
            {
                // add new
                cohortToAdd.AnimalType = this;
                cohortToAdd.AnimalTypeName = this.NameWithParent;

                Cohorts.Add(cohortToAdd);
            }
            else
                cohortexists.Number += cohortToAdd.Number;

            (Parent as OtherAnimals).LastCohortChanged = cohortToAdd;

            ReportTransaction(TransactionType.Gain, cohortToAdd.Number, activity, relatesToResource, category, this, cohortToAdd);
        }

        /// <summary>
        /// Remove individuals from type based on cohort
        /// </summary>
        /// <param name="removeIndividuals"></param>
        /// <param name="activity"></param>
        /// <param name="reason"></param>
        public void Remove(object removeIndividuals, CLEMModel activity, string reason)
        {
            OtherAnimalsTypeCohort cohortToRemove = removeIndividuals as OtherAnimalsTypeCohort;
            OtherAnimalsTypeCohort cohortexists = Cohorts.Where(a => a.Age == cohortToRemove.Age && a.Sex == cohortToRemove.Sex).First();

            double numberAdjusted = 0;
            if (cohortexists == null)
            {
                // tried to remove individuals that do not exist
                throw new Exception("Tried to remove individuals from " + this.Name + " that do not exist");
            }
            else
            {
                numberAdjusted = cohortToRemove.Number;
                cohortexists.Number -= cohortToRemove.Number;
                cohortexists.Number = Math.Max(0, cohortexists.Number);
            }

            (Parent as OtherAnimals).LastCohortChanged = cohortToRemove;
            ReportTransaction(TransactionType.Loss, numberAdjusted, activity, "", reason, this, cohortToRemove);
            if (cohortToRemove.Number == 0)
            {
                cohortToRemove.AdjustedNumber = 0;
                cohortToRemove.Age = 0;
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
