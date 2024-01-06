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

        #region Growth SCA version

        // TODO: provide summary
        /// <summary>
        /// 
        /// </summary>
        [Description("Feed heat production scalar")]
        public double FHPScalar { get; set; }

        // TODO: provide summary
        /// <summary>
        /// 
        /// </summary>
        [Description("Heat production viscera feed level")]
        public double HPVisceraFL { get; set; }

        // TODO: provide summary
        /// <summary>
        /// 
        /// </summary>
        [Description("SRW Slow growth factor [CN3]")]
        public double SRWSlowGrowthFactor { get; set; }

        /// <summary>
        /// First intercept of equation to determine energy fat mass (MJ day-1)
        /// </summary>
        [Description("Fat gain intercept number 1")]
        public double FatGainIntercept1 { get; set; }

        /// <summary>
        /// Second intercept of equation to determine energy fat mass (MJ day-1)
        /// </summary>
        [Description("Fat gain intercept number 2")]
        public double FatGainIntercept2 { get; set; }

        /// <summary>
        /// Slope of equation to determine energy fat mass (MJ day-1)
        /// </summary>
        [Description("Fat gain slope")]
        public double FatGainSlope { get; set; }

        /// <summary>
        /// Second intercept of equation to determine energy protein mass (MJ day-1)
        /// </summary>
        [Description("Protein gain intercept number 1")]
        public double ProteinGainIntercept1 { get; set; }

        /// <summary>
        /// First intercept of equation to determine energy protein mass (MJ day-1)
        /// </summary>
        [Description("Protein gain intercept number 2")]
        public double ProteinGainIntercept2 { get; set; }

        /// <summary>
        /// Slope of equation to determine energy protein mass (MJ day-1)
        /// </summary>
        [Description("Protein gain slope")]
        public double ProteinGainSlope { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double FetalNormWeightParameter { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double FetalNormWeightParameter2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ConceptusWeightRatio { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ConceptusWeightParameter { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ConceptusWeightParameter2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ConceptusEnergyContent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ConceptusEnergyParameter { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ConceptusEnergyParameter2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ConceptusProteinContent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ConceptusProteinParameter { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ConceptusProteinParameter2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double EB2LW { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double RumenDegradabilityIntercept { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double RumenDegradabilitySlope { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double RumenDegradableProteinIntercept { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double RumenDegradableProteinSlope { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double RumenDegradableProteinExponent { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double RumenDegradabilityConcentrateSlope { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double RumenDegradableProteinShortfallScalar { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double BreedEUPFactor1 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double BreedEUPFactor2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CN1 { get; set; } = 0.0115;

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CN2 { get; set; } = 0.27;

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double GrowthEnergySlope1 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double GrowthEnergySlope2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ProteinGainSlope1 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double ProteinGainSlope2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CL5 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CL6 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CG12 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double EBW2LW { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double Prt2NMilk { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double Prt2NTissue { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CA5 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CA7 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CA8 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CH1 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CH2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CH3 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CH4 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CH5 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CG4 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CG5 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CG6 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Description("")]
        public double CG7 { get; set; }

        /// <summary>
        /// Maintenance exponent for age [CM3]
        /// </summary>
        [Description("Maintenance exponent for age [CM3]")]
        public double CM3 { get; set; }

        /// <summary>
        /// Age effect min [CM4]
        /// </summary>
        [Description("Age effect min [CM4]")]
        public double CM4 { get; set; }

        /// <summary>
        /// Milk scalar [CM5]
        /// </summary>
        [Description("Milk scalar [CM5]")]
        public double CM5 { get; set; }



        #endregion

        #region Lactation SCA version

        /// <summary>
        /// Energy content of milk (MJ/L)
        /// </summary>
        [Description("Energy content of milk")]
        public double EnergyContentMilk { get; set; }

        /// <summary>
        /// Protein content of milk (kg/L)
        /// </summary>
        [Description("Protein content of milk")]
        public double ProteinContentMilk { get; set; }

        /// <summary>
        /// Lactation energy deficit (Clxx in SCA)
        /// </summary>
        [Description("Lactation energy deficit (Clxx)")]
        public double LactationEnergyDeficit { get; set; }
        
        /// <summary>
        /// Peak yield lactation scalar (Clxx in SCA)
        /// </summary>
        [Description("Peak lactation yield scalar (Clxx)")]
        public double PeakYieldScalar { get; set; }

        /// <summary>
        /// Potential yield lactation effect 1 (Clxx in SCA)
        /// </summary>
        [Description("Potential yield lactation effect 1 (Clxx)")]
        public double PotentialYieldLactationEffect { get; set; }

        /// <summary>
        /// Potential yield lactation effect 2 (Clxx in SCA)
        /// </summary>
        [Description("Potential yield lactation effect 1 (Clxx)")]
        public double PotentialYieldLactationEffect2 { get; set; }

        /// <summary>
        /// Potential lactation yield condition effect 1 (Clxx in SCA)
        /// </summary>
        [Description("Potential lactation yield condition effect (Clxx)")]
        public double PotentialYieldConditionEffect { get; set; }

        /// <summary>
        /// Potential lactation yield condition effect 2 (Clxx in SCA)
        /// </summary>
        [Description("Potential lactation yield condition effect 2 (Clxx)")]
        public double PotentialYieldConditionEffect2 { get; set; }

        /// <summary>
        /// Potential lactation yield MEI effect (Clxx in SCA)
        /// </summary>
        [Description("Potential lactation yield MEI effect (Clxx)")]
        public double PotentialYieldMEIEffect { get; set; }

        /// <summary>
        /// Potential lactation yield (Clxx in SCA)
        /// </summary>
        [Description("Potential lactation yield (Clxx)")]
        public double PotentialLactationYieldParameter { get; set; }

        /// <summary>
        /// Potential lactation yield reduction (Clxx in SCA)
        /// </summary>
        [Description("Potential lactation yield reduction (Clxx)")]
        public double PotentialYieldReduction { get; set; }

        /// <summary>
        /// Potential lactation yield reduction 2 (Clxx in SCA)
        /// </summary>
        [Description("Potential lactation yield reduction 2 (Clxx)")]
        public double PotentialYieldReduction2 { get; set; }

        /// <summary>
        /// Milk consumption limit 1 (Clxx in SCA)
        /// </summary>
        [Description("MilkConsumptionLimit1 (Clxx)")]
        public double MilkConsumptionLimit1 { get; set; }

        /// <summary>
        /// Milk consumption limit 2 (Clxx in SCA)
        /// </summary>
        [Description("Milk consumption limit 2 (Clxx)")]
        public double MilkConsumptionLimit2 { get; set; }

        /// <summary>
        /// Milk consumption limit 3 (Clxx in SCA)
        /// </summary>
        [Description("Milk consumption limit 3 (Clxx)")]
        public double MilkConsumptionLimit3 { get; set; }

        #endregion

        /// <summary>
        /// Unit type
        /// </summary>
        public string Units { get { return "NA"; } }

        /// <summary>
        /// Breed
        /// </summary>
        [Category("Basic", "General")]
        [Description("Breed")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of breed required")]
        public string Breed { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantType()
        {
            this.SetDefaults();
        }

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
                    return parentHerd.Herd.Where(a => a.HerdName == this.Name).Count();
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
                    return parentHerd.Herd.Where(a => a.HerdName == this.Name).Sum(a => a.AdultEquivalent);
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

        #region Grow Activity

        /// <summary>
        /// Energy maintenance efficiency coefficient
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy maintenance efficiency coefficient")]
        [Required, GreaterThanValue(0)]
        public double EMaintEfficiencyCoefficient { get; set; }
        /// <summary>
        /// Energy maintenance efficiency intercept
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy maintenance efficiency intercept")]
        [Required, GreaterThanValue(0)]
        public double EMaintEfficiencyIntercept { get; set; }
        /// <summary>
        /// Energy growth efficiency coefficient
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy growth efficiency coefficient")]
        [Required, GreaterThanValue(0)]
        public double EGrowthEfficiencyCoefficient { get; set; }
        /// <summary>
        /// Energy growth efficiency intercept
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy growth efficiency intercept")]
        [Required]
        public double EGrowthEfficiencyIntercept { get; set; }
        /// <summary>
        /// Energy lactation efficiency coefficient
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy lactation efficiency coefficient")]
        [Required, GreaterThanValue(0)]
        public double ELactationEfficiencyCoefficient { get; set; }
        /// <summary>
        /// Energy lactation efficiency intercept
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy lactation efficiency intercept")]
        [Required, GreaterThanValue(0)]
        public double ELactationEfficiencyIntercept { get; set; }
        /// <summary>
        /// Energy maintenance exponent
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy maintenance exponent")]
        [Required, GreaterThanValue(0)]
        public double EMaintExponent { get; set; }
        /// <summary>
        /// Energy maintenance intercept
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy maintenance intercept")]
        [Required, GreaterThanValue(0)]
        public double EMaintIntercept { get; set; }
        /// <summary>
        /// Energy maintenance coefficient
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy maintenance coefficient")]
        [Required, GreaterThanValue(0)]
        public double EMaintCoefficient { get; set; }
        /// <summary>
        /// Maximum age for energy maintenance calculation (yrs)
        /// </summary>
        //[System.ComponentModel.DefaultValueAttribute(6)]
        [Category("Advanced", "Growth")]
        [Description("Maximum age for energy maintenance calculation")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier EnergyMaintenanceMaximumAge { get; set; }
        /// <summary>
        /// Breed factor for maintenence energy
        /// </summary>
        [Category("Basic", "Growth")]
        [Description("Breed factor for maintenence energy")]
        [Required, GreaterThanValue(0)]
        public double Kme { get; set; }
        /// <summary>
        /// Parameter for calculation of energy needed per kg empty body gain #1 (a, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants)
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy per kg growth #1")]
        [Required, GreaterThanValue(0)]
        public double GrowthEnergyIntercept1 { get; set; }
        /// <summary>
        /// Parameter for calculation of energy needed per kg empty body gain #2 (b, see p37 Table 1.11 Nutrient Requirements of domesticated ruminants)
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Energy per kg growth #2")]
        [Required, GreaterThanValue(0)]
        public double GrowthEnergyIntercept2 { get; set; }

        /// <summary>
        /// Growth efficiency
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Growth efficiency")]
        [Required, GreaterThanValue(0)]
        public double GrowthEfficiency { get; set; }

        /// <summary>
        /// Natural weaning age
        /// </summary>
        [Category("Basic", "Growth")]
        [Description("Natural weaning age (0 to use gestation length)")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier NaturalWeaningAge { get; set; }

        /// <summary>
        /// Standard Reference Weight of female
        /// </summary>
        [Category("Basic", "General")]
        [Units("kg")]
        [Description("Standard Ref. Weight (kg) for a female")]
        [Required, GreaterThanValue(0)]
        public double SRWFemale { get; set; }
        /// <summary>
        /// Standard Reference Weight for male from female multiplier
        /// </summary>
        [Category("Advanced", "General")]
        [Description("Male Standard Ref. Weight multiplier from female")]
        [Required, GreaterThanValue(0)]
        public double SRWMaleMultiplier { get; set; }
        /// <summary>
        /// Standard Reference Weight at birth
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Units("Proportion of female SRW")]
        [Description("Birth mass as proportion of female SRW (singlet,twins,triplets..)")]
        [Required, GreaterThanValue(0), Proportion]
        public double[] BirthScalar { get; set; }
        /// <summary>
        /// Age growth rate coefficient
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Age growth rate coefficient")]
        [Required, GreaterThanValue(0)]
        public double AgeGrowthRateCoefficient { get; set; }
        /// <summary>
        /// SWR growth scalar
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("SRW growth scalar")]
        [Required, GreaterThanValue(0)]
        public double SRWGrowthScalar { get; set; }
        /// <summary>
        /// Relative body condition to score rate
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Rel. Body Cond. to Score rate")]
        [Required, GreaterThanValue(0)]
        public double RelBCToScoreRate { get; set; } = 0.15;
        /// <summary>
        /// Body condition score range
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Body Condition Score range (min, mid, max)")]
        [Required, ArrayItemCount(3)]
        public double[] BCScoreRange { get; set; } = { 0, 3, 5 };
        /// <summary>
        /// Body condition score to determine additional mortality
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Body Condition Score for additional mortality")]
        [Required]
        [System.ComponentModel.DefaultValue(0)]
        public double BodyConditionScoreForMortality { get; set; } = 0;
        /// <summary>
        /// Low body condition score to mortality rate
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Mortality rate for low Body Condition Score")]
        [Required]
        [System.ComponentModel.DefaultValue(0.5)]
        public double BodyConditionScoreMortalityRate { get; set; } = 0.5;
        /// <summary>
        /// Intake coefficient in relation to live weight
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Intake coefficient in relation to Live Weight")]
        [Required, GreaterThanValue(0)]
        public double IntakeCoefficient { get; set; }
        /// <summary>
        /// Intake intercept in relation to live weight
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Intake intercept in relation to SRW")]
        [Required, GreaterThanValue(0)]
        public double IntakeIntercept { get; set; }
        /// <summary>
        /// Potential intake modifier for maximum intake possible when overfeeding
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Potential intake modifer for max overfeeding intake")]
        [Required, GreaterThanEqualValue(1)]
        [System.ComponentModel.DefaultValue(1)]
        public double OverfeedPotentialIntakeModifier { get; set; }
        /// <summary>
        /// Protein requirement coeff (g/kg feed)
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Protein requirement coeff (g/kg feed)")]
        [Required, GreaterThanValue(0)]
        public double ProteinCoefficient { get; set; }
        /// <summary>
        /// Protein degradability
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Protein degradability")]
        [Required, GreaterThanValue(0)]
        public double ProteinDegradability { get; set; }
        /// <summary>
        /// Weight(kg) of 1 animal equivalent(steer)
        /// </summary>
        [Category("Basic", "General")]
        [Description("Weight (kg) of an animal equivalent")]
        [Required, GreaterThanValue(0)]
        public double BaseAnimalEquivalent { get; set; }
        /// <summary>
        /// Maximum green in diet
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Maximum green in diet")]
        [Required, Proportion]
        public double GreenDietMax { get; set; }
        /// <summary>
        /// Shape of curve for diet vs pasture
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Shape of curve for diet vs pasture")]
        [Required, GreaterThanValue(0)]
        public double GreenDietCoefficient { get; set; }
        /// <summary>
        /// Proportion green in pasture at zero in diet
        /// was %
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Proportion green in pasture at zero in diet")]
        [Required, Proportion]
        public double GreenDietZero { get; set; }
        /// <summary>
        /// Coefficient to adjust intake for herbage biomass
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Coefficient to adjust intake for herbage biomass")]
        [Required, GreaterThanValue(0)]
        public double IntakeCoefficientBiomass { get; set; }
        /// <summary>
        /// Enforce strict feeding limits
        /// </summary>
        [Category("Basic", "Diet")]
        [Description("Enforce strict feeding limits")]
        [Required]
        public bool StrictFeedingLimits { get; set; }
        /// <summary>
        /// Coefficient of juvenile milk intake
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Coefficient of juvenile milk intake")]
        [Required, GreaterThanValue(0)]
        public double MilkIntakeCoefficient { get; set; }
        /// <summary>
        /// Intercept of juvenile milk intake
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Intercept of juvenile milk intake")]
        [Required, GreaterThanValue(0)]
        public double MilkIntakeIntercept { get; set; }
        /// <summary>
        /// Maximum juvenile milk intake
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Maximum juvenile milk intake")]
        [Required, GreaterThanValue(0)]
        public double MilkIntakeMaximum { get; set; }
        /// <summary>
        /// Milk as proportion of LWT for fodder substitution
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Milk as proportion of LWT for fodder substitution")]
        [Required, Proportion]
        public double MilkLWTFodderSubstitutionProportion { get; set; }
        /// <summary>
        /// Max juvenile (suckling) intake as proportion of LWT
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Max juvenile (suckling) intake as proportion of LWT")]
        [Required, GreaterThanValue(0)]
        public double MaxJuvenileIntake { get; set; }
        /// <summary>
        /// Proportional discount to intake due to milk intake
        /// </summary>
        [Category("Advanced", "Diet")]
        [Description("Proportional discount to intake due to milk intake")]
        [Required, Proportion]
        public double ProportionalDiscountDueToMilk { get; set; }

        /// <summary>
        /// Style of calculating condition-based mortality
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Style of calculating additional condition-based mortality")]
        [System.ComponentModel.DefaultValue(ConditionBasedCalculationStyle.None)]
        [Required]
        public ConditionBasedCalculationStyle ConditionBasedMortalityStyle { get; set; }
        /// <summary>
        /// Cut-off for condition-based mortality
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Cut-off for condition-based mortality")]
        [Required]
        public double ConditionBasedMortalityCutOff { get; set; }
        /// <summary>
        /// Probability of dying if less than condition-based mortality cut-off
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Probability of death below condition-based cut-off")]
        [System.ComponentModel.DefaultValue(1)]
        [Required, GreaterThanValue(0)]
        public double ConditionBasedMortalityProbability { get; set; }

        /// <summary>
        /// Lactating Potential intake modifier Coefficient A
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Lactating potential intake modifier coefficient A")]
        [Required, GreaterThanValue(0)]
        public double LactatingPotentialModifierConstantA { get; set; }
        /// <summary>
        /// Lactating Potential intake modifier Coefficient B
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Lactating potential intake modifier coefficient B")]
        [Required, GreaterThanValue(0)]
        public double LactatingPotentialModifierConstantB { get; set; }
        /// <summary>
        /// Lactating Potential intake modifier Coefficient C
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Lactating potential intake modifier coefficient C")]
        [Required, GreaterThanValue(0)]
        public double LactatingPotentialModifierConstantC { get; set; }
        /// <summary>
        /// Maximum size of individual relative to SRW
        /// </summary>
        [Category("Advanced", "General")]
        [Description("Maximum size of individual relative to SRW")]
        [Required, GreaterThanValue(0)]
        public double MaximumSizeOfIndividual { get; set; }
        /// <summary>
        /// Mortality rate base
        /// </summary>
        [Category("Basic", "Survival")]
        [Description("Mortality rate base")]
        [Required, Proportion]
        public double MortalityBase { get; set; }
        /// <summary>
        /// Mortality rate coefficient
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Mortality rate coefficient")]
        [Required, GreaterThanValue(0)]
        public double MortalityCoefficient { get; set; }
        /// <summary>
        /// Mortality rate intercept
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Mortality rate intercept")]
        [Required, GreaterThanValue(0)]
        public double MortalityIntercept { get; set; }
        /// <summary>
        /// Mortality rate exponent
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Mortality rate exponent")]
        [Required, GreaterThanValue(0)]
        public double MortalityExponent { get; set; }
        /// <summary>
        /// Juvenile mortality rate coefficient
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Juvenile mortality rate coefficient")]
        [Required, GreaterThanValue(0)]
        public double JuvenileMortalityCoefficient { get; set; }
        /// <summary>
        /// Juvenile mortality rate maximum
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Juvenile mortality rate maximum")]
        [Required, Proportion]
        public double JuvenileMortalityMaximum { get; set; }
        /// <summary>
        /// Juvenile mortality rate exponent
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Juvenile mortality rate exponent")]
        [Required]
        public double JuvenileMortalityExponent { get; set; }
        /// <summary>
        /// Wool coefficient
        /// </summary>
        [Category("Advanced", "Products")]
        [Description("Wool coefficient")]
        [Required]
        public double WoolCoefficient { get; set; }
        /// <summary>
        /// Cashmere coefficient
        /// </summary>
        [Category("Advanced", "Products")]
        [Description("Cashmere coefficient")]
        [Required]
        public double CashmereCoefficient { get; set; }
        #endregion

        #region Breed activity

        /// <summary>
        /// Advanced conception parameters if present
        /// </summary>
        [JsonIgnore]
        public IConceptionModel ConceptionModel { get; set; }

        /// <summary>
        /// Milk curve shape suckling
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Milk curve shape suckling")]
        [Required, GreaterThanValue(0)]
        public double MilkCurveSuckling { get; set; }
        /// <summary>
        /// Milk curve shape non suckling
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Milk curve shape non suckling")]
        [Required, GreaterThanValue(0)]
        public double MilkCurveNonSuckling { get; set; }
        /// <summary>
        /// Number of days for milking
        /// </summary>
        [Category("Basic", "Lactation")]
        [Description("Number of days for milking")]
        [Required, GreaterThanEqualValue(0)]
        public double MilkingDays { get; set; }
        /// <summary>
        /// Peak milk yield(kg/day)
        /// </summary>
        [Category("Basic", "Lactation")]
        [Description("Peak milk yield (kg/day)")]
        [Required, GreaterThanValue(0)]
        public double MilkPeakYield { get; set; }
        /// <summary>
        /// Milk offset day
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Milk offset day")]
        [Required, GreaterThanValue(0)]
        public double MilkOffsetDay { get; set; }
        /// <summary>
        /// Milk peak day
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Milk peak day")]
        [Required, GreaterThanValue(0)]
        public double MilkPeakDay { get; set; }
        /// <summary>
        /// Proportion offspring born male
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0.5)]
        [Category("Advanced", "Breeding")]
        [Description("Proportion of offspring male")]
        [Required, Proportion]
        public double ProportionOffspringMale { get; set; }
        /// <summary>
        /// Inter-parturition interval intercept of PW (months)
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Inter-parturition interval intercept of PW (months)")]
        [Required, GreaterThanValue(0)]
        public double InterParturitionIntervalIntercept { get; set; }
        /// <summary>
        /// Inter-parturition interval coefficient of PW (months)
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Inter-parturition interval coefficient of PW (months)")]
        [Required]
        public double InterParturitionIntervalCoefficient { get; set; }
        /// <summary>
        /// Days between conception and parturition
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Days from conception to parturition")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier GestationLength { get; set; }
        /// <summary>
        /// Minimum age for 1st mating (months)
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Minimum age for 1st mating")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Units("years, months, days")]
        public AgeSpecifier MinimumAge1stMating { get; set; }
        /// <summary>
        /// Minimum size for 1st mating, proportion of SRW
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Minimum size for 1st mating, proportion of SRW")]
        [Required, Proportion]
        public double MinimumSize1stMating { get; set; }
        /// <summary>
        /// Minimum number of days between last birth and conception
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Minimum number of days between last birth and conception")]
        [Required, GreaterThanValue(0)]
        public double MinimumDaysBirthToConception { get; set; }
        /// <summary>
        /// Rate at which multiple births are concieved (twins, triplets, ...)
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Rate at which multiple births occur (twins,triplets,...")]
        [Proportion]
        public double[] MultipleBirthRate { get; set; }
        /// <summary>
        /// Proportion of SRW for zero calving/lambing rate
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Proportion of SRW required before conception possible (min size for mating)")]
        [Required, Proportion]
        public double CriticalCowWeight { get; set; }

        /// <summary>
        /// Maximum number of matings per male per day
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Maximum number of matings per male per day")]
        [Required, GreaterThanValue(0)]
        public double MaximumMaleMatingsPerDay { get; set; }
        /// <summary>
        /// Prenatal mortality rate
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Mortality rate from conception to birth (proportion)")]
        [Required, Proportion]
        public double PrenatalMortality { get; set; }

        /// <summary>
        /// Proportion of wet mother's with no offspring accepting orphan
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Proportion suitable fmeales accpeting orphan")]
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Required, Proportion]
        public double ProportionAcceptingSurrogate { get; set; } = 0;

        #endregion

        #region other

        // add intercept again if next methane equation requires an intercept value

        /// <summary>
        /// Methane production from intake coefficient
        /// </summary>
        [Category("Advanced", "Products")]
        [Description("Methane production from intake coefficient")]
        [Required, GreaterThanValue(0)]
        public double MethaneProductionCoefficient { get; set; }

        #endregion

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

            // ensure at least one conception model is associated
            int conceptionModelCount = this.FindAllChildren<Model>().Where(a => typeof(IConceptionModel).IsAssignableFrom(a.GetType())).Count();
            if (conceptionModelCount > 1)
            {
                string[] memberNames = new string[] { "RuminantType.IConceptionModel" };
                results.Add(new ValidationResult(String.Format("Only one Conception component is permitted below the Ruminant Type [r={0}]", Name), memberNames));
            }

            if (this.FindAllChildren<AnimalPricing>().Count() > 1)
            {
                string[] memberNames = new string[] { "RuminantType.Pricing" };
                results.Add(new ValidationResult(String.Format("Only one Animal pricing schedule is permitted within a Ruminant Type [{0}]", this.Name), memberNames));
            }
            else if (this.FindAllChildren<AnimalPricing>().Count() == 1)
            {
                AnimalPricing price = this.FindAllChildren<AnimalPricing>().FirstOrDefault() as AnimalPricing;

                if (price.FindAllChildren<AnimalPriceGroup>().Count() == 0)
                {
                    string[] memberNames = new string[] { "RuminantType.Pricing.RuminantPriceGroup" };
                    results.Add(new ValidationResult(String.Format("At least one Ruminant Price Group is required under an animal pricing within Ruminant Type [{0}]", this.Name), memberNames));
                }
            }

            if(BirthScalar.Length < MultipleBirthRate.Length-1)
            {
                string[] memberNames = new string[] { "RuminantType.BirthScalar" };
                results.Add(new ValidationResult($"The number of [BirthScalar] values [{BirthScalar.Length}] must must be one more than the number of [MultipleBirthRate] values [{MultipleBirthRate.Length}]. Birth rate scalars represent the size at birth relative to female SRW for singlet, twins, triplets etc and depend on the value provided for [MultipleBirthRate]", memberNames));
            }
            return results;
        }

        #endregion
    }
}