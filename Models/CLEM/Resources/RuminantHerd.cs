using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;  //enumerator
using Newtonsoft.Json;
using System.Runtime.Serialization;
using Models.Core;
using Models.Core.Attributes;
using Models.CLEM.Reporting;
using System.Globalization;
using Models.CLEM.Interfaces;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Groupings;

namespace Models.CLEM.Resources
{

    ///<summary>
    /// Parent model of Ruminant Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    //[ViewName("UserInterface.Views.PropertyCategorisedView")]
    //[PresenterName("UserInterface.Presenters.PropertyCategorisedMultiModelPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all rumiant types (herds or breeds) for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantHerd.htm")]
    public class RuminantHerd: ResourceBaseWithTransactions
    {
        /// <summary>
        /// Transaction grouping style
        /// </summary>
        [Description("Herd transactions grouping style")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Herd transactions grouping style required")]
        public RuminantTransactionsGroupingStyle TransactionStyle { get; set; }

        /// <summary>
        /// Current state of this resource.
        /// </summary>
        [JsonIgnore]
        public List<Ruminant> Herd;

        /// <summary>
        /// List of requested purchases.
        /// </summary>
        [JsonIgnore]
        public List<Ruminant> PurchaseIndividuals;

        /// <summary>
        /// The last individual to be added or removed (for reporting)
        /// </summary>
        [JsonIgnore]
        public object LastIndividualChanged { get; set; }

        /// <summary>
        /// The details of an individual for reporting
        /// </summary>
        [JsonIgnore]
        public RuminantReportItemEventArgs ReportIndividual { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            id = 1;
            Herd = new List<Ruminant>();
            PurchaseIndividuals = new List<Ruminant>();

            // for each Ruminant type 
            foreach (RuminantType rType in this.FindAllChildren<RuminantType>())
            {
                foreach (RuminantInitialCohorts ruminantCohorts in rType.FindAllChildren<RuminantInitialCohorts>())
                {
                    foreach (var ind in ruminantCohorts.CreateIndividuals())
                    {
                        ind.SaleFlag = HerdChangeReason.InitialHerd;
                        AddRuminant(ind, this);
                    }
                }
            }

            // Assign mothers to suckling calves
            foreach (string herdName in Herd.Select(a => a.HerdName).Distinct())
            {
                List<Ruminant> herd = Herd.Where(a => a.HerdName == herdName).ToList();

                if (herd.Any())
                {
                    // get list of all sucking individuals
                    var sucklingGroups = herd.Where(a => a.Weaned == false).GroupBy(a => a.Age).OrderByDescending(a => a.Key);

                    foreach (var sucklingList in sucklingGroups)
                    {
                        // get list of females of breeding age and condition
                        List<RuminantFemale> breedFemales = herd.OfType<RuminantFemale>().Where(a => a.Age >= a.BreedParams.MinimumAge1stMating + a.BreedParams.GestationLength + sucklingList.Key && a.Age <= a.BreedParams.MaximumAgeMating && a.HighWeight >= (a.BreedParams.MinimumSize1stMating * a.StandardReferenceWeight) && a.Weight >= (a.BreedParams.CriticalCowWeight * a.StandardReferenceWeight)).OrderByDescending(a => a.Age).ToList();

                        if (!breedFemales.Any())
                        {
                            if (sucklingList.Any())
                            {
                                Summary.WriteWarning(this, $"Insufficient breeding females to assign [{sucklingList.Count()}] [{sucklingList.Key}] month old sucklings for herd [r={herdName}].\r\nUnassigned calves will need to graze or be fed and may have reduced growth until weaned.\r\nBreeding females must be at least minimum breeding age + gestation length + age of sucklings at the start of the simulation to provide a calf.");
                                break;
                            }
                        }
                        else
                        {
                            // assign calves to cows
                            int sucklingCount = 0;
                            int numberThisPregnancy = breedFemales[0].CalulateNumberOfOffspringThisPregnancy();
                            int previousRuminantID = -1;
                            foreach (var suckling in sucklingList)
                            {
                                sucklingCount++;
                                if (breedFemales.Count > 0)
                                {
                                    // if next new female set up some details
                                    if (breedFemales[0].ID != previousRuminantID)
                                    {
                                        //Initialise female milk production in at birth so ready for sucklings to consume
                                        double milkTime = (suckling.Age * 30.4) + 15; // +15 equivalent to mid month production

                                        // need to calculate normalised animal weight here for milk production
                                        double milkProduction = breedFemales[0].BreedParams.MilkPeakYield * breedFemales[0].Weight / breedFemales[0].NormalisedAnimalWeight * (Math.Pow(((milkTime + breedFemales[0].BreedParams.MilkOffsetDay) / breedFemales[0].BreedParams.MilkPeakDay), breedFemales[0].BreedParams.MilkCurveSuckling)) * Math.Exp(breedFemales[0].BreedParams.MilkCurveSuckling * (1 - (milkTime + breedFemales[0].BreedParams.MilkOffsetDay) / breedFemales[0].BreedParams.MilkPeakDay));
                                        breedFemales[0].MilkProduction = Math.Max(milkProduction, 0.0);
                                        breedFemales[0].MilkCurrentlyAvailable = milkProduction * 30.4;

                                        // generalised curve
                                        // previously * 30.64
                                        double currentIPI = Math.Pow(breedFemales[0].BreedParams.InterParturitionIntervalIntercept * (breedFemales[0].Weight / breedFemales[0].StandardReferenceWeight), breedFemales[0].BreedParams.InterParturitionIntervalCoefficient);
                                        // restrict minimum period between births
                                        currentIPI = Math.Max(currentIPI, breedFemales[0].BreedParams.GestationLength + 2);

                                        //breedFemales[0].Parity = breedFemales[0].Age - suckling.Age - 9;
                                        // AL removed the -9 as this would make it conception month not birth month
                                        breedFemales[0].AgeAtLastBirth = breedFemales[0].Age - suckling.Age;
                                        breedFemales[0].AgeAtLastConception = breedFemales[0].AgeAtLastBirth - breedFemales[0].BreedParams.GestationLength;
                                        breedFemales[0].SetAgeEnteredSimulation(breedFemales[0].AgeAtLastConception);
                                    }

                                    // add this offspring to birth count
                                    if (suckling.Age == 0)
                                    {
                                        breedFemales[0].NumberOfBirthsThisTimestep++;
                                    }

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
                                    {
                                        numberThisPregnancy--;
                                    }
                                }
                                else
                                {
                                    Summary.WriteWarning(this, $"Insufficient breeding females to assign [{sucklingList.Count() - sucklingCount}] [{sucklingList.Key}] month old sucklings for herd [r={herdName}].\r\nUnassigned calves will need to graze or be fed and may have reduced growth until weaned.\r\nBreeding females must be at least minimum breeding age + gestation length + age of sucklings at the start of the simulation to provide a calf.");
                                    break;
                                }
                            }

                        }
                    }

                    // gestation interval at smallest size generalised curve
                    double minAnimalWeight = herd[0].StandardReferenceWeight - ((1 - herd[0].BreedParams.SRWBirth) * herd[0].StandardReferenceWeight) * Math.Exp(-(herd[0].BreedParams.AgeGrowthRateCoefficient * (herd[0].BreedParams.MinimumAge1stMating * 30.4)) / (Math.Pow(herd[0].StandardReferenceWeight, herd[0].BreedParams.SRWGrowthScalar)));
                    double minsizeIPI = Math.Pow(herd[0].BreedParams.InterParturitionIntervalIntercept * (minAnimalWeight / herd[0].StandardReferenceWeight), herd[0].BreedParams.InterParturitionIntervalCoefficient);
                    // restrict minimum period between births
                    minsizeIPI = Math.Max(minsizeIPI, herd[0].BreedParams.GestationLength + 2);

                    // assigning values for the remaining females who haven't just bred.
                    // i.e met breeding rules and not pregnant or lactating (just assigned calf), but calculate for underweight individuals not previously provided calves.
                    double ageFirstBirth = herd[0].BreedParams.MinimumAge1stMating + herd[0].BreedParams.GestationLength;
                    foreach (RuminantFemale female in herd.OfType<RuminantFemale>().Where(a => !a.IsLactating && !a.IsPregnant && (a.Age >= a.BreedParams.MinimumAge1stMating + a.BreedParams.GestationLength & a.HighWeight >= a.BreedParams.MinimumSize1stMating * a.StandardReferenceWeight)))
                    {
                        // generalised curve
                        double currentIPI = Math.Pow(herd[0].BreedParams.InterParturitionIntervalIntercept * (female.Weight / female.StandardReferenceWeight), herd[0].BreedParams.InterParturitionIntervalCoefficient);
                        // restrict minimum period between births (previously +61)
                        currentIPI = Math.Max(currentIPI, female.BreedParams.GestationLength + 2);

                        // calculate number of births assuming conception at min age first mating
                        // therefore first birth min age + gestation length

                        int numberOfBirths = Convert.ToInt32((female.Age - ageFirstBirth) / ((currentIPI + minsizeIPI) / 2), CultureInfo.InvariantCulture) - 1;
                        female.AgeAtLastBirth = ageFirstBirth + (currentIPI * numberOfBirths);
                        female.AgeAtLastConception = female.AgeAtLastBirth - female.BreedParams.GestationLength;

                        // no longer needed as only work with stats during the simulation.
                        // fill breeding stats prior to simulation start
                        // assumes all previous births successful
                        //female.NumberOfConceptions = female.NumberOfBirths;
                        //female.NumberOfOffspring = female.NumberOfBirths;
                        //female.NumberOfWeaned = female.NumberOfBirths;
                    }
                }
            }
            // group herd ready for reporting
            groupedHerdForReporting = SummarizeIndividualsByGroups(Herd, PurchaseOrSalePricingStyleType.Purchase);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfSimulation")]
        private void OnEndOfSimulation(object sender, EventArgs e)
        {
            // report all females of breeding age at end of simulation
            foreach (RuminantFemale female in Herd.Where(a => a.Sex == Sex.Female && a.Age >= a.BreedParams.MinimumAge1stMating))
            {
                RuminantReportItemEventArgs args = new RuminantReportItemEventArgs
                {
                    RumObj = female,
                    Category = "breeding stats"
                };
                OnFinalFemaleOccurred(args);
            }
        }

        /// <summary>
        /// Add individual/cohort to the the herd
        /// </summary>
        /// <param name="ind">Individual Ruminant to add</param>
        /// <param name="model">Model adding individual</param>
        public void AddRuminant(Ruminant ind, IModel model)
        {
            if (ind.ID == 0)
            {
                ind.ID = this.NextUniqueID;
            }
            Herd.Add(ind);
            LastIndividualChanged = ind;

            // check mandatory attributes
            ind.BreedParams.CheckMandatoryAttributes(ind, model);

            ResourceTransaction details = new ResourceTransaction
            {
                TransactionType = TransactionType.Gain,
                Amount = 1,
                Activity = model as CLEMModel,
                Category = ind.SaleFlag.ToString(),
                ResourceType = ind.BreedParams,
                RelatesToResource = ind.BreedParams.NameWithParent,
                ExtraInformation = ind
            };
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);

            // remove change flag
            ind.SaleFlag = HerdChangeReason.None;
        }

        /// <summary>
        /// Remove individual/cohort from the herd
        /// </summary>
        /// <param name="ind">Individual Ruminant to remove</param>
        /// <param name="model">Model removing individual</param>
        public void RemoveRuminant(Ruminant ind, IModel model)
        {
            // Remove mother ID from any suckling offspring
            if (ind.Sex == Sex.Female)
            {
                foreach (var offspring in (ind as RuminantFemale).SucklingOffspringList)
                {
                    offspring.Mother = null;
                }
            }
            // if sold and unweaned set mothers weaning count + 1 as effectively weaned in process and not death
            if (!ind.Weaned & !ind.SaleFlag.ToString().Contains("Died"))
            {
                if(ind.Mother != null)
                {
                    ind.Mother.NumberOfWeaned++;
                }
            }

            Herd.Remove(ind);
            LastIndividualChanged = ind;

            // report transaction of herd change
            ResourceTransaction details = new ResourceTransaction
            {
                TransactionType = TransactionType.Loss,
                Amount = 1,
                Activity = model as CLEMModel,
                Category = ind.SaleFlag.ToString(),
                ResourceType = ind.BreedParams,
                RelatesToResource = ind.BreedParams.NameWithParent,
                ExtraInformation = ind
            };
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);

            // report female breeding stats if needed
            if(ind.Sex == Sex.Female & ind.Age >= ind.BreedParams.MinimumAge1stMating)
            {
                RuminantReportItemEventArgs args = new RuminantReportItemEventArgs
                {
                    RumObj = ind,
                    Category = "breeding stats"
                };
                OnFinalFemaleOccurred(args);
            }

            // remove change flag
            ind.SaleFlag = HerdChangeReason.None;
        }

        /// <summary>
        /// Statstical summar of a list of numbers (e.g. attribute values)
        /// </summary>
        [JsonIgnore]
        public ListStatistics LastListStatistics { get; set; }

        /// <summary>
        /// Return the mean and standard deviation of an attribute value
        /// </summary>
        public int SummariseAttribute(string tag, bool ignoreNotFound)
        {
            LastListStatistics = new ListStatistics();
            if (Herd is null)
            {
                return 0;
            }

            var values = Herd.Where( a => (ignoreNotFound & a.Attributes.GetValue(tag) == null) ? false : true).Select(a => Convert.ToDouble(a.Attributes.GetValue(tag)?.storedValue));
            if (values.Count() == 0)
            {
                return 0;
            }
            double sd = 0;
            double mean = values.Average();
            double sum = values.Sum(d => Math.Pow(d - mean, 2));
            sd = Math.Sqrt((sum) / values.Count() - 1);
            LastListStatistics.Average = mean;
            LastListStatistics.StandardDeviation = sd;
            LastListStatistics.Count = values.Count();
            LastListStatistics.Total = Herd.Count();
            return Herd.Count();
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (Herd != null)
            {
                Herd.Clear();
            }
            Herd = null;
            if (PurchaseIndividuals != null)
            {
                PurchaseIndividuals.Clear();
            }
            PurchaseIndividuals = null;
        }

        /// <summary>
        /// Remove list of Ruminants from the herd
        /// </summary>
        /// <param name="list">List of Ruminants to remove</param>
        /// <param name="model">Model removing individuals</param>
        public void RemoveRuminant(List<Ruminant> list, IModel model)
        {
            foreach (var ind in list)
            {
                // report removal
                RemoveRuminant(ind, model);
            }
        }

        /// <summary>
        /// Gte the next unique individual id number
        /// </summary>
        public int NextUniqueID { get { return id++; } }
        private int id = 1;

        #region group tracking

        /// <summary>
        /// Access to the herd grouped by transaction style for reporting in FinalizeTimeStep before EndTimeStep
        /// </summary>
        private IEnumerable<RuminantReportTypeDetails> groupedHerdForReporting;

        /// <summary>
        /// Overrides the base class method to allow for changes before end of month reporting
        /// </summary>
        [EventSubscribe("CLEMHerdSummary")]
        private void OnCLEMHerdSummary(object sender, EventArgs e)
        {
            // group herd ready for reporting
            // performed at herd summary to avoid end of step aging purchases etc
            groupedHerdForReporting = SummarizeIndividualsByGroups(Herd, PurchaseOrSalePricingStyleType.Purchase);
        }

        /// <summary>
        /// Get the specific report group with details to report from the grouped herd
        /// </summary>
        /// <param name="ruminantTypeName">Name of ruminant type</param>
        /// <param name="groupName">Name of group category</param>
        /// <returns>The group details</returns>
        public RuminantReportGroupDetails GetRuminantReportGroup(string ruminantTypeName, string groupName)
        {
            if(groupedHerdForReporting.Any())
            {
                var rumGroup = groupedHerdForReporting.FirstOrDefault(a => a.RuminantTypeName == ruminantTypeName);
                if(rumGroup != null)
                {
                    var catGroup = rumGroup.RuminantTypeGroup.FirstOrDefault(a => a.GroupName == groupName);
                    if (catGroup != null)
                    {
                        return catGroup;
                    }
                }
            }
            return new RuminantReportGroupDetails() { Count = 0, TotalAdultEquivalent = 0, TotalWeight = 0, TotalPrice = 0, GroupName = groupName };
        }

        /// <summary>
        /// Generate the store for tracking individuals in groups for reporting
        /// </summary>
        /// <returns>Dicitonary of ResourceTypes and categories for each</returns>
        public IEnumerable<string> GetReportingGroups(RuminantType ruminantType)
        {
            List<string> catNames = new List<string>();
            switch (TransactionStyle)
            {
                case RuminantTransactionsGroupingStyle.Combined:
                    catNames.Add("All");
                    break;
                case RuminantTransactionsGroupingStyle.ByPriceGroup:
                    var animalPricing = ruminantType.FindAllChildren<AnimalPricing>().FirstOrDefault();
                    if (animalPricing != null)
                    {
                        catNames.AddRange(animalPricing.FindAllChildren<AnimalPriceGroup>().Select(a => a.Name));
                    }
                    break;
                case RuminantTransactionsGroupingStyle.ByClass:
                    catNames.AddRange(Enum.GetNames(typeof(RuminantClass)));
                    break;
                case RuminantTransactionsGroupingStyle.BySexAndClass:
                    var classes = Enum.GetNames(typeof(RuminantClass));
                    foreach (var item in classes)
                    {
                        switch (item)
                        {
                            case "Castrate":
                            case "Sire":
                                catNames.Add($"{item}Male");
                                break;
                            default:
                                catNames.Add($"{item}Female");
                                catNames.Add($"{item}Male");
                                break;
                        }
                    }
                    break;
            default:
                    break;
            }
            return catNames;
        }

        /// <summary>
        /// Group and summarize individuals by transaction style for reporting
        /// </summary>
        /// <param name="individuals">Individuals to summarize</param>
        /// <param name="priceStyle">Price style to use</param>
        /// <returns>A grouped summary of individuals</returns>
        public IEnumerable<RuminantReportTypeDetails> SummarizeIndividualsByGroups(IEnumerable<Ruminant> individuals, PurchaseOrSalePricingStyleType priceStyle)
        {
            var groupedInd = from ind in individuals
                                    group ind by ind.BreedParams.Name into breedGroup
                                    select new RuminantReportTypeDetails()
                                    {
                                        RuminantTypeName = breedGroup.Key,
                                        RuminantTypeGroup = from gind in breedGroup
                                                 group gind by gind.GetTransactionCategory(TransactionStyle, priceStyle) into catind
                                                 select new RuminantReportGroupDetails()
                                                 {
                                                     GroupName = catind.Key,
                                                     Count = catind.Count(),
                                                     TotalAdultEquivalent = catind.Sum(a => a.AdultEquivalent),
                                                     TotalWeight = catind.Sum(a => a.Weight),
                                                     TotalPrice = catind.Sum(a => a.BreedParams.ValueofIndividual(a, priceStyle)?.CalculateValue(a))
                                                 }
                                    };
            return groupedInd;
        }

        #endregion 

        #region Transactions

        // Must be included away from base class so that APSIM Event.Subscriber can find them 

        /// <summary>
        /// Override base event
        /// </summary>
        protected new void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public new event EventHandler TransactionOccurred;

        private void Resource_TransactionOccurred(object sender, EventArgs e)
        {
            LastTransaction = (e as TransactionEventArgs).Transaction;
            OnTransactionOccurred(e);
        }

        #endregion

        #region weaning event

        /// <summary>
        /// Override base event
        /// </summary>
        public void OnWeanOccurred(EventArgs e)
        {
            ReportIndividual = e as RuminantReportItemEventArgs;
            WeanOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public event EventHandler WeanOccurred;

        private void Resource_WeanOccurred(object sender, EventArgs e)
        {
            OnWeanOccurred(e);
        }

        #endregion

        #region breeding female left herd event

        /// <summary>
        /// Override base event
        /// </summary>
        public void OnFinalFemaleOccurred(EventArgs e)
        {
            ReportIndividual = e as RuminantReportItemEventArgs;
            FinalFemaleOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Override base event
        /// </summary>
        public event EventHandler FinalFemaleOccurred;

        private void Resource_FinalFemaleOccurred(object sender, EventArgs e)
        {
            OnFinalFemaleOccurred(e);
        }

        #endregion

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\r\n<div class=\"activityentry\">Activities reporting on herds will group individuals";
            switch (TransactionStyle)
            {
                case RuminantTransactionsGroupingStyle.Combined:
                    html += " into a single transaction per RuminantType.";
                    break;
                case RuminantTransactionsGroupingStyle.ByPriceGroup:
                    html += " by the pricing groups provided for the RuminantType.";
                    break;
                case RuminantTransactionsGroupingStyle.ByClass:
                    html += " by the class of individual.";
                    break;
                default:
                    break;
            }
            html += "</div>";
            return html;
        }

        #endregion
    }

    /// <summary>
    /// A list of the ruminant type groups found in herd
    /// </summary>
    public class RuminantReportTypeDetails
    {
        /// <summary>
        /// Name of ruminant type
        /// </summary>
        public string RuminantTypeName { get; set; }

        /// <summary>
        /// A list of all the details for the type
        /// </summary>
        public IEnumerable<RuminantReportGroupDetails> RuminantTypeGroup { get; set; }
    }

    /// <summary>
    /// Details of a ruminant reporting group
    /// </summary>
    public class RuminantReportGroupDetails
    {
        /// <summary>
        /// Name of group
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// Number of individuals
        /// </summary>
        public int? Count { get; set; }

        /// <summary>
        /// Sum of adult equivalents
        /// </summary>
        public double? TotalAdultEquivalent { get; set; }

        /// <summary>
        /// Sum of weight
        /// </summary>
        public double? TotalWeight { get; set; }

        /// <summary>
        /// Sum of price
        /// </summary>
        public double? TotalPrice { get; set; }

        /// <summary>
        /// Average adult equivalents
        /// </summary>
        public double AverageAdultEquivalent { get { return (TotalAdultEquivalent??0.0) / Convert.ToDouble(Count??1); } }

        /// <summary>
        /// Average weight
        /// </summary>
        public double AverageWeight { get { return (TotalWeight ?? 0.0) / Convert.ToDouble(Count ?? 1); } }

        /// <summary>
        /// Average price
        /// </summary>
        public double AveragePrice { get { return (TotalPrice ?? 0.0) / Convert.ToDouble(Count ?? 1); } }

    }
}
