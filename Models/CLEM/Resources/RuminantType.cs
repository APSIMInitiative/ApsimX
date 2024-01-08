using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters for a ruminant Type
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantHerd))]
    [Description("This resource represents a ruminant type (e.g. Bos indicus breeding herd)")]
    [Version(1, 0, 5, "Parameters moved to individual child models")]
    [Version(1, 0, 4, "Added parameter for overfeed potential intake multiplier")]
    [Version(1, 0, 3, "Added parameter for proportion offspring that are male")]
    [Version(1, 0, 2, "All conception parameters moved to associated conception components")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantType.htm")]
    public class RuminantType : CLEMResourceTypeBase, IValidatableObject, IResourceType
    {
        private RuminantHerd parentHerd = null;
        private List<AnimalPriceGroup> priceGroups = new();
        private readonly List<string> mandatoryAttributes = new();
        private readonly List<string> warningsMultipleEntry = new();
        private readonly List<string> warningsNotFound = new();

        /// <summary>
        /// Store of parameters
        /// </summary>
        [JsonIgnore]
        public RuminantParameters Parameters { get; set; } = new RuminantParameters();

        /// <summary>
        /// Advanced conception parameters if present
        /// </summary>
        [JsonIgnore]
        public IConceptionModel ConceptionModel { get; set; }

        /// <summary>
        /// Unit type
        /// </summary>
        public string Units { get { return "NA"; } }

        /// <summary>
        /// Current value of individuals in the herd
        /// </summary>
        [JsonIgnore]
        public AnimalPricing PriceList;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            Parameters.Initialise(this);
            parentHerd = this.Parent as RuminantHerd;

            // clone pricelist so model can modify if needed and not affect initial parameterisation
            if (FindAllChildren<AnimalPricing>().Any())
            {
                PriceList = this.FindAllChildren<AnimalPricing>().FirstOrDefault();
                // Components are not permanently modifed during simulation so no need for clone: PriceList = Apsim.Clone(this.FindAllChildren<AnimalPricing>().FirstOrDefault()) as AnimalPricing;
                priceGroups = PriceList.FindAllChildren<AnimalPriceGroup>().Cast<AnimalPriceGroup>().ToList();
            }

            // get conception parameters and rate calculation method
            ConceptionModel = this.FindAllChildren<Model>().Where(a => typeof(IConceptionModel).IsAssignableFrom(a.GetType())).Cast<IConceptionModel>().FirstOrDefault();
        }

        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Determine if a price schedule has been provided for this breed
        /// </summary>
        /// <returns>boolean</returns>
        public bool PricingAvailable() { return (PriceList != null); }

        /// <summary>
        /// Property indicates whether to include attribute inheritance when mating
        /// </summary>
        public bool IncludedAttributeInheritanceWhenMating { get { return (mandatoryAttributes.Any()); } }

        /// <summary>
        /// Add a attribute name to the list of mandatory attributes for the type
        /// </summary>
        /// <param name="name">name of attribute</param>
        public void AddMandatoryAttribute(string name)
        {
            if (!mandatoryAttributes.Contains(name))
                mandatoryAttributes.Add(name);
        }

        /// <summary>
        /// Determins whether a specified attribute is mandatory
        /// </summary>
        /// <param name="name">name of attribute</param>
        public bool IsMandatoryAttribute(string name)
        {
            return mandatoryAttributes.Contains(name);
        }

        /// <summary>
        /// Check whether an individual has all mandotory attributes
        /// </summary>
        /// <param name="ind">Individual ruminant to check</param>
        /// <param name="model">Model adding individuals</param>
        public void CheckMandatoryAttributes(Ruminant ind, IModel model)
        {
            foreach (var attribute in mandatoryAttributes)
            {
                if (!ind.Attributes.Exists(attribute))
                {
                    string warningString = $"No mandatory attribute [{attribute.ToUpper()}] present for individual added by [a={model.Name}]";
                    Warnings.CheckAndWrite(warningString, Summary, this, MessageType.Error);
                }
            }
        }

        /// <summary>
        /// Get value of a specific individual
        /// </summary>
        /// <returns>value</returns>
        public AnimalPriceGroup GetPriceGroupOfIndividual(Ruminant ind, PurchaseOrSalePricingStyleType purchaseStyle, string warningMessage = "")
        {
            if (PricingAvailable())
            {
                AnimalPriceGroup animalPrice = (purchaseStyle == PurchaseOrSalePricingStyleType.Purchase) ? ind.CurrentPriceGroups.Buy : ind.CurrentPriceGroups.Sell;
                if (animalPrice == null || !animalPrice.Filter(ind))
                {
                    // search through RuminantPriceGroups for first match with desired purchase or sale flag
                    foreach (AnimalPriceGroup priceGroup in priceGroups.Where(a => a.PurchaseOrSale == purchaseStyle || a.PurchaseOrSale == PurchaseOrSalePricingStyleType.Both))
                        if (priceGroup.Filter(ind))
                        {
                            if (purchaseStyle == PurchaseOrSalePricingStyleType.Purchase)
                            {
                                ind.CurrentPriceGroups = (priceGroup, ind.CurrentPriceGroups.Sell);
                                return priceGroup;
                            }
                            else
                            {
                                ind.CurrentPriceGroups = (ind.CurrentPriceGroups.Buy, priceGroup);
                                return priceGroup;
                            }
                        }

                    // no price match found.
                    string warningString = warningMessage;
                    if (warningString == "")
                        warningString = $"No [{purchaseStyle}] price entry was found for [r={ind.Breed}] meeting the required criteria [f=age: {ind.AgeInDays}] [f=sex: {ind.Sex}] [f=weight: {ind.Weight:##0}]";
                    Warnings.CheckAndWrite(warningString, Summary, this, MessageType.Warning);
                }
                return animalPrice;
            }
            return null;
        }

        /// <summary>
        /// Get value of a specific individual with special requirements check (e.g. breeding sire or draught purchase)
        /// </summary>
        /// <returns>value</returns>
        public AnimalPriceGroup GetPriceGroupOfIndividual(Ruminant ind, PurchaseOrSalePricingStyleType purchaseStyle, string property, string value, string warningMessage = "")
        {
            double price = 0;
            if (PricingAvailable())
            {
                AnimalPriceGroup animalPrice = (purchaseStyle == PurchaseOrSalePricingStyleType.Purchase) ? ind.CurrentPriceGroups.Buy : ind.CurrentPriceGroups.Sell;
                if (animalPrice == null || !animalPrice.Filter(ind))
                {
                    string criteria = property.ToUpper() + ":" + value.ToUpper();

                    //find first pricing entry matching specific criteria
                    AnimalPriceGroup matchIndividual = null;
                    AnimalPriceGroup matchCriteria = null;

                    var priceGroups = PriceList.FindAllChildren<AnimalPriceGroup>()
                        .Where(a => a.PurchaseOrSale == purchaseStyle || a.PurchaseOrSale == PurchaseOrSalePricingStyleType.Both);

                    foreach (AnimalPriceGroup priceGroup in priceGroups)
                    {
                        if (priceGroup.Filter(ind) && matchIndividual == null)
                            matchIndividual = priceGroup;

                        var suitableFilters = priceGroup.FindAllChildren<FilterByProperty>()
                            .Where(a => (a.PropertyOfIndividual == property) &
                            (
                                (a.Operator == System.Linq.Expressions.ExpressionType.Equal && a.Value.ToString().ToUpper() == value.ToUpper()) |
                                (a.Operator == System.Linq.Expressions.ExpressionType.NotEqual && a.Value.ToString().ToUpper() != value.ToUpper()) |
                                (a.Operator == System.Linq.Expressions.ExpressionType.IsTrue && value.ToUpper() == "TRUE") |
                                (a.Operator == System.Linq.Expressions.ExpressionType.IsFalse && value.ToUpper() == "FALSE")
                            )
                            ).Any();

                        // check that pricing item meets the specified criteria.
                        if (suitableFilters)
                        {
                            if (matchCriteria == null)
                                matchCriteria = priceGroup;
                            else
                            {
                                // multiple price entries were found. using first. value = xxx.
                                if (!warningsMultipleEntry.Contains(criteria))
                                {
                                    warningsMultipleEntry.Add(criteria);
                                    Summary.WriteMessage(this, "Multiple specific [" + purchaseStyle.ToString() + "] price entries were found for [r=" + ind.Breed + "] where [" + property + "]" + (value.ToUpper() != "TRUE" ? " = [" + value + "]." : ".") + "\r\nOnly the first entry will be used. Price [" + matchCriteria.Value.ToString("#,##0.##") + "] [" + matchCriteria.PricingStyle.ToString() + "].", MessageType.Warning);
                                }
                            }
                        }
                    }

                    if (matchCriteria == null)
                    {
                        string warningString = warningMessage;
                        if (warningString != "")
                        {
                            // no warning string passed to method so calculate one
                            // report specific criteria not found in price list
                            warningString = "No [" + purchaseStyle.ToString() + "] price entry was found for [r=" + ind.Breed + "] meeting the required criteria [" + property + "]" + (value.ToUpper() != "TRUE" ? " = [" + value + "]." : ".");

                            if (matchIndividual != null)
                            {
                                // add using the best pricing available for [][] purchases of xx per head
                                warningString += "\r\nThe best available price [" + matchIndividual.Value.ToString("#,##0.##") + "] [" + matchIndividual.PricingStyle.ToString() + "] will be used.";
                                price = matchIndividual.Value * ((matchIndividual.PricingStyle == PricingStyleType.perKg) ? ind.Weight : 1.0);
                            }
                            else
                                warningString += "\r\nNo alternate price for individuals could be found for the individuals. Add a new [r=AnimalPriceGroup] entry in the [r=AnimalPricing] for [" + ind.Breed + "]";
                        }

                        if (!warningsNotFound.Contains(criteria))
                        {
                            warningsNotFound.Add(criteria);
                            Summary.WriteMessage(this, warningString, MessageType.Warning);
                        }
                    }
                    if (purchaseStyle == PurchaseOrSalePricingStyleType.Purchase)
                    {
                        ind.CurrentPriceGroups = (matchCriteria, ind.CurrentPriceGroups.Sell);
                        return matchCriteria;
                    }
                    else
                    {
                        ind.CurrentPriceGroups = (ind.CurrentPriceGroups.Buy, matchCriteria);
                        return matchCriteria;
                    }
                }
            }
            return null;
        }

        #region transactions

        /// <summary>
        /// Add resource
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove resource
        /// </summary>
        /// <param name="request"></param>
        public new void Remove(ResourceRequest request)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set resource
        /// </summary>
        /// <param name="newAmount"></param>
        public new void Set(double newAmount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initialise resource
        /// </summary>
        public void Initialise()
        {
            throw new NotImplementedException();
        }

        #endregion

        /// <summary>
        /// Current number of individuals of this herd.
        /// </summary>
        public double Amount
        {
            get
            {
                if (parentHerd != null)
                    return parentHerd.Herd.Where(a => a.HerdName == Name).Count();
                return 0;
            }
        }

        /// <summary>
        /// Current number of individuals of this herd.
        /// </summary>
        public double AmountAE
        {
            get
            {
                if (parentHerd != null)
                    return parentHerd.Herd.Where(a => a.HerdName == Name).Sum(a => a.AdultEquivalent);
                return 0;
            }
        }

        /// <summary>
        /// Arguments for the recent conception status change for OnConceptionStatusChanged
        /// </summary>
        [JsonIgnore]
        public ConceptionStatusChangedEventArgs LastConceptionStatus { get; set; }

        /// <summary>
        /// The conception status of a female changed for advanced reporting
        /// </summary>
        public event EventHandler ConceptionStatusChanged;

        /// <summary>
        /// Conception status changed 
        /// </summary>
        /// <param name="e"></param>
        public void OnConceptionStatusChanged(ConceptionStatusChangedEventArgs e)
        {
            LastConceptionStatus = e;
            ConceptionStatusChanged?.Invoke(this, e);
        }

        #region descriptive summary 

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string html = "";
            return html;
        }

        #endregion

        #region validation

        /// <summary>
        /// Model Validation
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // ensure multiple conception model are not provided associated. COnception model can be missing if no breeding required.
            //int conceptionModelCount = this.FindAllChildren<Model>().Where(a => typeof(IConceptionModel).IsAssignableFrom(a.GetType())).Count();
            if (FindAllChildren<IConceptionModel>().Count() > 1)
            {
                string[] memberNames = new string[] { "RuminantType.IConceptionModel" };
                results.Add(new ValidationResult($"Only one Conception component is permitted below the Ruminant Type [r={Name}]", memberNames));
            }

            IEnumerable<AnimalPricing> pricing = FindAllChildren<AnimalPricing>();
            if (pricing.Count() > 1)
            {
                string[] memberNames = new string[] { "RuminantType.Pricing" };
                results.Add(new ValidationResult($"Only one Animal pricing schedule is permitted within a Ruminant Type [{Name}]", memberNames));
            }
            else if (pricing.Count() == 1)
            {
                if (!pricing.FirstOrDefault().FindAllChildren<AnimalPriceGroup>().Any())
                {
                    string[] memberNames = new string[] { "RuminantType.Pricing.RuminantPriceGroup" };
                    results.Add(new ValidationResult($"At least one Ruminant Price Group is required under an animal pricing within Ruminant Type [{Name}]", memberNames));
                }
            }
            return results;
        }

        #endregion
    }
}