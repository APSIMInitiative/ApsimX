using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Groupings;
using Models.Core.Attributes;
using Models.CLEM.Reporting;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters for a ruminant Type
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyTreeView")]
    [PresenterName("UserInterface.Presenters.PropertyTreePresenter")]
    [ValidParent(ParentType = typeof(RuminantHerd))]
    [Description("This resource represents a ruminant type (e.g. Bos indicus breeding herd). It can be used to define different breeds in the sumulation or different herds (e.g. breeding and trade herd) within a breed that will be managed differently.")]
    [Version(1, 0, 3, "Added parameter for proportion offspring that are male")]
    [Version(1, 0, 2, "All conception parameters moved to associated conception components")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantType.htm")]
    public class RuminantType : CLEMResourceTypeBase, IValidatableObject, IResourceType
    {
        [Link]
        private ResourcesHolder Resources = null;

        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get {return "NA"; }  }

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
        [XmlIgnore]
        public AnimalPricing PriceList;

        private List<AnimalPriceGroup> priceGroups = new List<AnimalPriceGroup>();

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            // clone pricelist so model can modify if needed and not affect initial parameterisation
            if(Apsim.Children(this, typeof(AnimalPricing)).Count() > 0)
            {
                PriceList = (Apsim.Children(this, typeof(AnimalPricing)).FirstOrDefault() as AnimalPricing).Clone();

                priceGroups = Apsim.Children(PriceList, typeof(AnimalPriceGroup)).Cast<AnimalPriceGroup>().ToList();
            }

            // get conception parameters and rate calculation method
            ConceptionModel = Apsim.Children(this, typeof(Model)).Where(a => typeof(IConceptionModel).IsAssignableFrom(a.GetType())).Cast<IConceptionModel>().FirstOrDefault();
        }

        /// <summary>
        /// Determine if a price schedule has been provided for this breed
        /// </summary>
        /// <returns>boolean</returns>
        public bool PricingAvailable() {  return (PriceList != null); }

        private readonly List<string> WarningsMultipleEntry = new List<string>();
        private readonly List<string> WarningsNotFound = new List<string>();

        /// <summary>
        /// Get value of a specific individual
        /// </summary>
        /// <returns>value</returns>
        public double ValueofIndividual(Ruminant ind, PurchaseOrSalePricingStyleType purchaseStyle)
        {
            if (PricingAvailable())
            {
                List<Ruminant> animalList = new List<Ruminant>() { ind };

                // search through RuminantPriceGroups for first match with desired purchase or sale flag

                foreach (AnimalPriceGroup item in priceGroups.Where(a => a.PurchaseOrSale == purchaseStyle || a.PurchaseOrSale == PurchaseOrSalePricingStyleType.Both))
                {
                    if (animalList.Filter(item).Count() == 1)
                    {
                        return item.Value * ((item.PricingStyle == PricingStyleType.perKg) ? ind.Weight : 1.0);
                    }
                }

                // no price match found.
                string warningString = $"No [{purchaseStyle.ToString()}] price entry was found for [r={ind.Breed}] meeting the required criteria [f=age: {ind.Age}] [f=gender: {ind.GenderAsString}] [f=weight: {ind.Weight.ToString("##0")}]";

                if (!Warnings.Exists(warningString))
                {
                    Warnings.Add(warningString);
                    Summary.WriteWarning(this, warningString);
                }
            }
            return 0;
        }

        /// <summary>
        /// Get value of a specific individual with special requirements check (e.g. breeding sire or draught purchase)
        /// </summary>
        /// <returns>value</returns>
        public double ValueofIndividual(Ruminant ind, PurchaseOrSalePricingStyleType purchaseStyle, RuminantFilterParameters property, string value)
        {
            double price = 0;
            if (PricingAvailable())
            {
                string criteria = property.ToString().ToUpper() + ":" + value.ToUpper();
                List<Ruminant> animalList = new List<Ruminant>() { ind };

                //find first pricing entry matching specific criteria
                AnimalPriceGroup matchIndividual = null;
                AnimalPriceGroup matchCriteria = null;
                foreach (AnimalPriceGroup item in Apsim.Children(PriceList, typeof(AnimalPriceGroup)).Cast<AnimalPriceGroup>().Where(a => a.PurchaseOrSale == purchaseStyle || a.PurchaseOrSale == PurchaseOrSalePricingStyleType.Both))
                {
                    if (animalList.Filter(item).Count() == 1 && matchIndividual == null)
                    {
                        matchIndividual = item;
                    }

                    // check that pricing item meets the specified criteria.
                    if (Apsim.Children(item, typeof(RuminantFilter)).Cast<RuminantFilter>().Where(a => (a.Parameter.ToString().ToUpper() == property.ToString().ToUpper() && a.Value.ToUpper() == value.ToUpper())).Count() > 0)
                    {
                        if (matchCriteria == null)
                        {
                            matchCriteria = item;
                        }
                        else
                        {
                            // multiple price entries were found. using first. value = xxx.
                            if (!WarningsMultipleEntry.Contains(criteria))
                            {
                                WarningsMultipleEntry.Add(criteria);
                                Summary.WriteWarning(this, "Multiple specific [" + purchaseStyle.ToString() + "] price entries were found for [r=" + ind.Breed + "] where [" + property + "]" + (value.ToUpper() != "TRUE" ? " = [" + value + "]." : ".")+"\nOnly the first entry will be used. Price [" + matchCriteria.Value.ToString("#,##0.##") + "] [" + matchCriteria.PricingStyle.ToString() + "].");
                            }
                        }
                    }
                }

                if(matchCriteria == null)
                {
                    // report specific criteria not found in price list
                    string warningString = "No [" + purchaseStyle.ToString() + "] price entry was found for [r=" + ind.Breed + "] meeting the required criteria [" + property + "]"+ (value.ToUpper() != "TRUE" ? " = [" + value + "]." : ".");

                    if(matchIndividual != null)
                    {
                        // add using the best pricing available for [][] purchases of xx per head
                        warningString += "\nThe best available price [" + matchIndividual.Value.ToString("#,##0.##") + "] ["+matchIndividual.PricingStyle.ToString()+ "] will be used.";
                        price = matchIndividual.Value * ((matchIndividual.PricingStyle == PricingStyleType.perKg) ? ind.Weight : 1.0);
                    }
                    else
                    {
                        warningString += "\nNo alternate price for individuals could be found for the individuals. Add a new [r=AnimalPriceGroup] entry in the [r=AnimalPricing] for [" +ind.Breed+"]";
                    }
                    if (!WarningsNotFound.Contains(criteria))
                    {
                        WarningsNotFound.Add(criteria);
                        Summary.WriteWarning(this, warningString);
                    }
                }
                else
                {
                    price = matchCriteria.Value * ((matchCriteria.PricingStyle == PricingStyleType.perKg) ? ind.Weight : 1.0);
                }
            }
            return price;
        }

        /// <summary>
        /// Add resource
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="reason">Name of individual adding resource</param>
        public new void Add(object resourceAmount, CLEMModel activity, string reason)
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

        /// <summary>
        /// Model Validation
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // ensure at least one conception model is associated
            int conceptionModelCount = Apsim.Children(this, typeof(Model)).Where(a => typeof(IConceptionModel).IsAssignableFrom(a.GetType())).Count();
            if (conceptionModelCount > 1)
            {
                string[] memberNames = new string[] { "RuminantType.IConceptionModel" };
                results.Add(new ValidationResult(String.Format("Only one Conception component is permitted below the Ruminant Type [r={0}]", Name), memberNames));
            }

            if (Apsim.Children(this, typeof(AnimalPricing)).Count() > 1)
            {
                string[] memberNames = new string[] { "RuminantType.Pricing" };
                results.Add(new ValidationResult(String.Format("Only one Animal pricing schedule is permitted within a Ruminant Type [{0}]", this.Name), memberNames));
            }
            else if (Apsim.Children(this, typeof(AnimalPricing)).Count() == 1)
            {
                AnimalPricing price = Apsim.Children(this, typeof(AnimalPricing)).FirstOrDefault() as AnimalPricing;

                if (Apsim.Children(price, typeof(AnimalPriceGroup)).Count()==0)
                {
                    string[] memberNames = new string[] { "RuminantType.Pricing.RuminantPriceGroup" };
                    results.Add(new ValidationResult(String.Format("At least one Ruminant Price Group is required under an animal pricing within Ruminant Type [{0}]", this.Name), memberNames));
                }
            }
            return results;
        }

        /// <summary>
        /// Current number of individuals of this herd.
        /// </summary>
        public double Amount
        {
            get
            {
                if (Resources.RuminantHerd().Herd != null)
                {
                    return Resources.RuminantHerd().Herd.Where(a => a.HerdName == this.Name).Count();
                }
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
                if (Resources.RuminantHerd().Herd != null)
                {
                    return Resources.RuminantHerd().Herd.Where(a => a.HerdName == this.Name).Sum(a => a.AdultEquivalent);
                }
                return 0;
            }
        }

        /// <summary>
        /// Returns the most recent conception status
        /// </summary>
        [XmlIgnore]
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
        [System.ComponentModel.DefaultValueAttribute(6)]
        [Category("Advanced", "Growth")]
        [Description("Maximum age for energy maintenance calculation (yrs)")]
        [Required, GreaterThanValue(0)]
        public double EnergyMaintenanceMaximumAge { get; set; }
        /// <summary>
        /// Breed factor for maintenence energy
        /// </summary>
        [Category("Basic", "Growth")]
        [Description("Breed factor for maintenence energy")]
        [Required, GreaterThanValue(0)]
        public double Kme { get; set; }
        /// <summary>
        /// Parameter for energy for growth #1
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Parameter for energy for growth #1")]
        [Required, GreaterThanValue(0)]
        public double GrowthEnergyIntercept1 { get; set; }
        /// <summary>
        /// Parameter for energy for growth #2
        /// </summary>
        [Category("Advanced", "Growth")]
        [Description("Parameter for energy for growth #2")]
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
        [Category("Advanced", "Growth")]
        [Description("Natural weaning age (0 to use gestation length)")]
        [Required]
        public double NaturalWeaningAge { get; set; }

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
        [Units("proportion of female SRW")]
        [Description("Birth mass (proportion of female SRW)")]
        [Required, GreaterThanValue(0)]
        public double SRWBirth { get; set; }
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
        /// Proportion of max body weight needed for survival
        /// </summary>
        [Category("Advanced", "Survival")]
        [Description("Proportion of max body weight needed for survival")]
        [Required, Proportion]
        public double ProportionOfMaxWeightToSurvive { get; set; }
        /// <summary>
        /// Lactating Potential intake modifier Coefficient A
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Lactating Potential intake modifier Coefficient A")]
        [Required, GreaterThanValue(0)]
        public double LactatingPotentialModifierConstantA { get; set; }
        /// <summary>
        /// Lactating Potential intake modifier Coefficient B
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Lactating Potential intake modifier Coefficient B")]
        [Required, GreaterThanValue(0)]
        public double LactatingPotentialModifierConstantB { get; set; }
        /// <summary>
        /// Lactating Potential intake modifier Coefficient C
        /// </summary>
        [Category("Advanced", "Lactation")]
        [Description("Lactating Potential intake modifier Coefficient C")]
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
        [XmlIgnore]
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
        /// Months between conception and parturition
        /// </summary>
        [Category("Advanced", "Breeding")]
        [Description("Months between conception and parturition")]
        [Required, GreaterThanValue(0)]
        public double GestationLength { get; set; }
        /// <summary>
        /// Minimum age for 1st mating (months)
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Minimum age for 1st mating (months)")]
        [Required, GreaterThanValue(0)]
        public double MinimumAge1stMating { get; set; }
        /// <summary>
        /// Maximum age for mating (months)
        /// </summary>
        [Category("Basic", "Breeding")]
        [Description("Maximum female age for mating")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(120)]
        public double MaximumAgeMating { get; set; }
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
        [Description("Proportion of SRW for zero Calving/lambing rate")]
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

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            return html;
        }
    }




}