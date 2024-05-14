using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
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
            //bool error = false;
            parentHerd = this.Parent as RuminantHerd;

            //var missingParameters = CLEMRuminantActivityBase.CheckRuminantParametersExist(this, new() { typeof(RuminantParametersGeneral) });
            //foreach (var error in missingParameters)
            //{
            //    Summary.WriteMessage(this, error, MessageType.Error);
            //}
            //if (missingParameters.Any())
            //    return;

            bool error = false;
            // check parameters are available for all ruminants.
            if (Parameters.General is null)
            {
                Summary.WriteMessage(this, $"Missing [RuminantParameters].[RuminantParametersGeneral] parameters for [{NameWithParent}]", MessageType.Error);
                error = true;
            }

            if (error)
                throw new ApsimXException(this, "Missing [RuminantParameter] components! See CLEM Component for details.");

            // clone pricelist so model can modify if needed and not affect initial parameterisation
            if (FindAllChildren<AnimalPricing>().Any())
            {
                PriceList = this.FindAllChildren<AnimalPricing>().FirstOrDefault();
                // Components are not permanently modifed during simulation so no need for clone: PriceList = Apsim.Clone(this.FindAllChildren<AnimalPricing>().FirstOrDefault()) as AnimalPricing;
                priceGroups = PriceList.FindAllChildren<AnimalPriceGroup>().Cast<AnimalPriceGroup>().ToList();
            }

            // get conception parameters and rate calculation method
            ConceptionModel = this.FindAllChildren<Model>().Where(a => typeof(IConceptionModel).IsAssignableFrom(a.GetType())).Cast<IConceptionModel>().FirstOrDefault();

            CLEMEvents events = FindInScope<CLEMEvents>();

            foreach (RuminantInitialCohorts ruminantCohorts in FindAllChildren<RuminantInitialCohorts>())
                foreach (var ind in ruminantCohorts.CreateIndividuals(events?.Clock.Start ?? default))
                {
                    ind.SaleFlag = HerdChangeReason.InitialHerd;
                    parentHerd.AddRuminant(ind, this);
                }

            // get list of all sucking individuals
            var sucklingGroups = parentHerd.Herd.Where(a => a.HerdName == Name && a.Weaned == false).GroupBy(a => a.AgeInDays).OrderBy(a => a.Key);

            if(sucklingGroups.Any())
            {
                // check parameters are available for all ruminants.
                if (Parameters.Breeding is null)
                {
                    Summary.WriteMessage(this, $"No [RuminantParametersFeed] parameters for [{NameWithParent}]{Environment.NewLine}All [RuminantType] require a [RuminantParametersFeed] component when a [RuminantActivityFeed] is used.", MessageType.Error);
                    return;
                }
            }

            foreach (IGrouping<int, Ruminant> sucklingList in sucklingGroups)
            {
                // get list of females of breeding age and condition
                List<RuminantFemale> breedFemales = parentHerd.Herd.OfType<RuminantFemale>().Where(a => a.HerdName == Name && a.AgeInDays > a.Parameters.General.MinimumAge1stMating.InDays + a.Parameters.General.GestationLength.InDays + sucklingList.Key && a.Weight.HighestAttained >= (a.Parameters.General.MinimumSize1stMating * a.Weight.StandardReferenceWeight) && a.Weight.Live >= (a.Parameters.Breeding.CriticalCowWeight * a.Weight.StandardReferenceWeight)).OrderByDescending(a => a.AgeInDays).ToList();

                // assign calves to cows
                int sucklingCount = 0;
                int numberThisPregnancy = breedFemales[0].CalulateNumberOfOffspringThisPregnancy();
                int previousRuminantID = -1;
                foreach (var suckling in sucklingList)
                {
                    sucklingCount++;
                    if (breedFemales.Any())
                    {
                        // if next new female set up some details
                        if (breedFemales[0].ID != previousRuminantID)
                        {
                            //ToDo: Is this really needed, or will it be calculated in Grow when needed.
                            // It is difficult to access Grow parameters here as we don't know which activity is being used.

                            ////Initialise female milk production in at birth so ready for sucklings to consume
                            ////ToDo: CLOCK age plus half interval for sucklings
                            //double milkTime = (suckling.AgeInDays) + events.Interval/2.0; // +15 equivalent to mid month production

                            //// need to calculate normalised animal weight here for milk production
                            //double milkProduction = breedFemales[0].Parameters.Breeding.MilkPeakYield * breedFemales[0].Weight / breedFemales[0].NormalisedAnimalWeight * (Math.Pow(((milkTime + breedFemales[0].BreedParams.MilkOffsetDay) / breedFemales[0].BreedParams.MilkPeakDay), breedFemales[0].BreedParams.MilkCurveSuckling)) * Math.Exp(breedFemales[0].BreedParams.MilkCurveSuckling * (1 - (milkTime + breedFemales[0].BreedParams.MilkOffsetDay) / breedFemales[0].BreedParams.MilkPeakDay));
                            //breedFemales[0].MilkProduction = Math.Max(milkProduction, 0.0);
                            //breedFemales[0].MilkCurrentlyAvailable = milkProduction * 30.4;

                            // generalised curve
                            // previously * 30.64
                            double currentIPI = Math.Pow(breedFemales[0].Parameters.Breeding.InterParturitionIntervalIntercept * (breedFemales[0].Weight.Live / breedFemales[0].Weight.StandardReferenceWeight), breedFemales[0].Parameters.Breeding.InterParturitionIntervalCoefficient);
                            // restrict minimum period between births
                            currentIPI = Math.Max(currentIPI, breedFemales[0].Parameters.General.GestationLength.InDays + 60);

                            breedFemales[0].DateOfLastBirth = events.GetTimeStepRangeContainingDate(breedFemales[0].DateOfBirth.AddDays(breedFemales[0].AgeInDays - suckling.AgeInDays)).start;
                            breedFemales[0].DateLastConceived = events.GetTimeStepRangeContainingDate(breedFemales[0].DateOfBirth.AddDays(-breedFemales[0].Parameters.General.GestationLength.InDays)).start;
                            breedFemales[0].DateEnteredSimulation = breedFemales[0].DateLastConceived;
                        }

                        // add this offspring to birth count
                        if (suckling.AgeInDays == 0)
                            breedFemales[0].NumberOfBirthsThisTimestep++;

                        // suckling mother set
                        suckling.Mother = breedFemales[0];
                        // add suckling to suckling offspring of mother.
                        breedFemales[0].SucklingOffspringList.Add(suckling);

                        // add this suckling to mother's offspring count.
                        breedFemales[0].NumberOfOffspring++;

                        // check if a twin and if so apply next individual to same mother.
                        // otherwise remove this mother from the list and change counters
                        if (numberThisPregnancy == 1)
                        {
                            breedFemales[0].NumberOfBirths++;
                            breedFemales[0].NumberOfConceptions = 1;
                            breedFemales.RemoveAt(0);
                        }
                        else
                            numberThisPregnancy--;
                    }
                    else
                    {
                        Summary.WriteMessage(this, $"Insufficient breeding females to assign [{sucklingList.Count() - sucklingCount}] x [{sucklingList.Key}] month old sucklings for herd [r={NameWithParent}].\r\nUnassigned calves will need to graze or be fed and may have reduced growth until weaned.\r\nBreeding females must be at least minimum breeding age + gestation length + age of sucklings at the start of the simulation to provide a suckling.", MessageType.Warning);
                        break;
                    }
                }

            }

            var remainingFemales = parentHerd.Herd.OfType<RuminantFemale>().Where(a => !a.IsLactating && !a.IsPregnant && (a.AgeInDays > a.Parameters.General.MinimumAge1stMating.InDays + a.Parameters.General.GestationLength.InDays & a.Weight.HighestAttained >= a.Parameters.General.MinimumSize1stMating * a.Weight.StandardReferenceWeight));
            if (remainingFemales.Any())
            {
                var firstFemale = remainingFemales.First();
                // gestation interval at smallest size generalised curve
                double minAnimalWeight = firstFemale.Weight.StandardReferenceWeight - ((1 - firstFemale.Parameters.General.BirthScalar[0]) * firstFemale.Weight.StandardReferenceWeight) * Math.Exp(-(firstFemale.Parameters.General.AgeGrowthRateCoefficient_CN1 * (firstFemale.Parameters.General.MinimumAge1stMating.InDays)) / (Math.Pow(firstFemale.Weight.StandardReferenceWeight, firstFemale.Parameters.General.SRWGrowthScalar_CN2))); ;
                double minsizeIPI = Math.Pow(firstFemale.Parameters.Breeding.InterParturitionIntervalIntercept * (minAnimalWeight / firstFemale.Weight.StandardReferenceWeight), firstFemale.Parameters.Breeding.InterParturitionIntervalCoefficient);
                // restrict minimum period between births
                minsizeIPI = Math.Max(minsizeIPI, firstFemale.Parameters.General.GestationLength.InDays + 2);

                // assigning values for the remaining females who haven't just bred.
                // i.e met breeding rules and not pregnant or lactating (just assigned suckling), but calculate for underweight individuals not previously provided sucklings.
                double ageFirstBirth = firstFemale.Parameters.General.MinimumAge1stMating.InDays + firstFemale.Parameters.General.GestationLength.InDays;
                foreach (RuminantFemale female in remainingFemales)
                {
                    // generalised curve
                    double currentIPI = Math.Pow(female.Parameters.Breeding.InterParturitionIntervalIntercept * (female.Weight.Live / female.Weight.StandardReferenceWeight), female.Parameters.Breeding.InterParturitionIntervalCoefficient);
                    // restrict minimum period between births (previously +61)
                    currentIPI = Math.Max(currentIPI, female.Parameters.General.GestationLength.InDays + 60);

                    // calculate number of births assuming conception at min age first mating
                    // therefore first birth min age + gestation length

                    int numberOfBirths = Convert.ToInt32((female.AgeInDays - ageFirstBirth) / ((currentIPI + minsizeIPI) / 2), CultureInfo.InvariantCulture) - 1;
                    female.DateOfLastBirth = events.GetTimeStepRangeContainingDate(female.DateOfBirth.AddDays(ageFirstBirth + (currentIPI * numberOfBirths))).start;
                    female.DateLastConceived = events.GetTimeStepRangeContainingDate(female.DateOfLastBirth.AddDays(-female.Parameters.General.GestationLength.InDays)).start;
                }
            }
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
                                price = matchIndividual.Value * ((matchIndividual.PricingStyle == PricingStyleType.perKg) ? ind.Weight.Live : 1.0);
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
                    return parentHerd.Herd.Where(a => a.HerdName == Name).Sum(a => a.Weight.AdultEquivalent);
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