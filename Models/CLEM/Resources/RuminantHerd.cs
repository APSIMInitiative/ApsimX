using Docker.DotNet.Models;
using DocumentFormat.OpenXml.Spreadsheet;
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
    ///<summary>
    /// Parent model of the herd of Ruminant Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("Resource group for all rumiant types (herds or breeds) in the simulation")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantHerd.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGeneral) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.DescendentOfRuminantType })]
    public class RuminantHerd : ResourceBaseWithTransactions, IValidatableObject
    {
        private int id = 1;

        /// <summary>
        /// Access to the herd grouped by transaction style for reporting in FinalizeTimeStep before EndTimeStep
        /// </summary>
        private IEnumerable<RuminantReportTypeDetails> groupedHerdForReporting;

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
        public Ruminant LastIndividualChanged { get; set; }

        /// <summary>
        /// The details of an individual for reporting
        /// </summary>
        [JsonIgnore]
        public RuminantReportItemEventArgs ReportIndividual { get; set; }

        /// <summary>
        /// Get the next unique individual id number
        /// </summary>
        public int NextUniqueID { get { return id++; } }

        /// <summary>
        /// The ruminant grow activity used in the simulation
        /// </summary>
        [JsonIgnore]
        public IRuminantActivityGrow RuminantGrowActivity { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            id = 1;
            Herd = new List<Ruminant>();
            PurchaseIndividuals = new List<Ruminant>();
            RuminantGrowActivity = FindInScope<IRuminantActivityGrow>();

            foreach (RuminantType rType in this.FindAllChildren<RuminantType>())
                rType.Parameters.Initialise(rType);
        }

        /// <summary>An event handler to allow us to perform final initialise after RuminantTypes have intialised.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivities")]
        private void OnCLEMInitialiseActivities(object sender, EventArgs e)
        {
            // group herd ready for reporting
            string warnMessage = $"Some ruminants did not have a [PriceGroup] of style [Purchase] for reporting value in a [Herd Summary].{System.Environment.NewLine}The values reported will not include these individuals. Ensure all individuals have a purchase price in order to provide ruminant value in summary reports.";
            groupedHerdForReporting = SummarizeIndividualsByGroups(Herd, PurchaseOrSalePricingStyleType.Purchase, warnMessage);
        }

        /// <summary>An event handler to allow us to peform atsks at the end of the simulation</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfSimulation")]
        private void OnEndOfSimulation(object sender, EventArgs e)
        {
            // report all females of breeding age at end of simulation
            RuminantReportItemEventArgs args = new()
            {
                Category = "breeding stats"
            };

            foreach (RuminantFemale female in Herd.OfType<RuminantFemale>().Where(a => a.AgeInDays >= a.Parameters.General.MinimumAge1stMating.InDays))
            {
                args.RumObj = female;
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
                ind.ID = NextUniqueID;

            Herd.Add(ind);
            LastIndividualChanged = ind;

            // check mandatory attributes
            ind.Parameters.Details.CheckMandatoryAttributes(ind, model);

            LastTransaction.TransactionType = TransactionType.Gain;
            LastTransaction.Amount = 1;
            LastTransaction.Activity = model as CLEMModel;
            LastTransaction.RelatesToResource = null;
            LastTransaction.Category = ind.SaleFlag.ToString();
            LastTransaction.ResourceType = ind.Parameters.Details;
            LastTransaction.ExtraInformation = ind;

            OnTransactionOccurred(null);

            // remove change flag
            ind.SaleFlag = HerdChangeReason.None;
        }

        /// <summary>
        /// Remove list of Ruminants from the herd
        /// </summary>
        /// <param name="list">List of Ruminants to remove</param>
        /// <param name="model">Model removing individuals</param>
        public void RemoveRuminant(IEnumerable<Ruminant> list, IModel model)
        {
            foreach (var ind in list.ToList())
                // report removal
                RemoveRuminant(ind, model);
        }

        /// <summary>
        /// Remove individual/cohort from the herd
        /// </summary>
        /// <param name="ind">Individual Ruminant to remove</param>
        /// <param name="model">Model removing individual</param>
        public void RemoveRuminant(Ruminant ind, IModel model)
        {
            // Remove mother ID from any suckling offspring
            if (ind is RuminantFemale)
            {
                while ((ind as RuminantFemale).SucklingOffspringList.Any())
                {
                    Ruminant offspring = (ind as RuminantFemale).SucklingOffspringList.FirstOrDefault();
                    offspring.MotherLost();
                }
            }

            // if sold and unweaned set mothers weaning count + 1 as effectively weaned in process and not death
            if (!ind.IsWeaned & !ind.SaleFlag.ToString().Contains("Died"))
            {
                if (ind.Mother != null)
                {
                    ind.Mother.SucklingOffspringList.Remove(ind);
                    ind.Mother.NumberOfWeaned++;
                }
            }

            Herd.Remove(ind);
            LastIndividualChanged = ind;

            LastTransaction.TransactionType = TransactionType.Loss;
            LastTransaction.Amount = 1;
            LastTransaction.Activity = model as CLEMModel;
            LastTransaction.RelatesToResource = null;
            LastTransaction.Category = ind.SaleFlag.ToString();
            LastTransaction.ResourceType = ind.Parameters.Details;
            LastTransaction.ExtraInformation = ind;

            OnTransactionOccurred(null);

            // report female breeding stats if needed
            if (ind.Sex == Sex.Female && ind.AgeInDays >= ind.Parameters.General.MinimumAge1stMating.InDays)
            {
                RuminantReportItemEventArgs args = new()
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
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private new void OnSimulationCompleted(object sender, EventArgs e)
        {
            Herd?.Clear();
            Herd = null;
            PurchaseIndividuals?.Clear();
            PurchaseIndividuals = null;
        }

        ///<inheritdoc/>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // clear purchased individuals at start of time step as there is no carryover
            // this is not the responsibility of any activity as we cannbe assured of what activities will be run.
            PurchaseIndividuals?.Clear();
        }

        #region group tracking

        /// <summary>
        /// Overrides the base class method to allow for changes before end of month reporting
        /// </summary>
        [EventSubscribe("CLEMHerdSummary")]
        private void OnCLEMHerdSummary(object sender, EventArgs e)
        {
            // group herd ready for reporting
            // performed at herd summary to avoid end of step aging purchases etc
            string warnMessage = $"Some ruminants did not have a [PriceGroup] of style [Purchase] for reporting value in a [Herd Summary].{System.Environment.NewLine}The values reported will not include these individuals. Ensure all individuals have a purchase price in order to provide ruminant value in summary reports.";
            groupedHerdForReporting = SummarizeIndividualsByGroups(Herd, PurchaseOrSalePricingStyleType.Purchase, warnMessage);
        }

        /// <summary>
        /// Get the specific report group with details to report from the grouped herd
        /// </summary>
        /// <param name="ruminantTypeName">Name of ruminant type</param>
        /// <param name="groupName">Name of group category</param>
        /// <returns>The group details</returns>
        public RuminantReportGroupDetails GetRuminantReportGroup(string ruminantTypeName, string groupName)
        {
            if (groupedHerdForReporting.Any())
            {
                var rumGroup = groupedHerdForReporting.FirstOrDefault(a => a.RuminantTypeName == ruminantTypeName);
                if (rumGroup != null)
                {
                    if (groupName == "") // blank requests the totals across all groups if present.
                    {
                        return new RuminantReportGroupDetails
                        {
                            GroupName = "All",
                            TotalAdultEquivalent = rumGroup.RuminantTypeGroup.Sum(a => a.TotalAdultEquivalent),
                            TotalPrice = rumGroup.RuminantTypeGroup.Sum(a => a.TotalPrice),
                            TotalWeight = rumGroup.RuminantTypeGroup.Sum(a => a.TotalWeight),
                            Count = rumGroup.RuminantTypeGroup.Sum(a => a.Count)
                        };
                    }
                    else
                    {
                        var catGroup = rumGroup.RuminantTypeGroup.FirstOrDefault(a => a.GroupName == groupName);
                        if (catGroup != null)
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
            List<string> catNames = new();
            switch (TransactionStyle)
            {
                case RuminantTransactionsGroupingStyle.Combined:
                    catNames.Add("All");
                    break;
                case RuminantTransactionsGroupingStyle.ByPriceGroup:
                    var animalPricing = ruminantType.FindAllChildren<AnimalPricing>().FirstOrDefault();
                    if (animalPricing != null)
                        catNames.AddRange(animalPricing.FindAllChildren<AnimalPriceGroup>().Select(a => a.Name));
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
        /// <param name="warningMessage">A custom warning message used if prices cannot be found otherwise the standard messge will be reported for each unique missing price</param>
        /// <returns>A grouped summary of individuals</returns>
        public IEnumerable<RuminantReportTypeDetails> SummarizeIndividualsByGroups(IEnumerable<Ruminant> individuals, PurchaseOrSalePricingStyleType priceStyle, string warningMessage = "")
        {
            bool multi = individuals.Select(a => a.Parameters.Details.Name).Distinct().Count() > 1;
            var groupedInd = from ind in individuals
                             group ind by ind.Parameters.Details.Name into breedGroup
                             select new RuminantReportTypeDetails()
                             {
                                 RuminantTypeName = breedGroup.Key,
                                 RuminantTypeNameToDisplay = (multi ? breedGroup.Key : ""),
                                 RuminantTypeGroup = from gind in breedGroup
                                                     group gind by gind.GetTransactionCategory(TransactionStyle, priceStyle) into catind
                                                     select new RuminantReportGroupDetails()
                                                     {
                                                         GroupName = catind.Key,
                                                         Count = catind.Count(),
                                                         TotalAdultEquivalent = catind.Sum(a => a.Weight.AdultEquivalent),
                                                         TotalWeight = catind.Sum(a => a.Weight.Live),
                                                         TotalPrice = catind.Sum(a => a.Parameters.Details.GetPriceGroupOfIndividual(a, priceStyle, warningMessage)?.CalculateValue(a))
                                                     }
                             };
            return groupedInd;
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

        /// <inheritdoc/>
        public override string ModelSummary()
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
                    html += " by the class of individuals.";
                    break;
                case RuminantTransactionsGroupingStyle.BySexAndClass:
                    html += " by the sex and class of individuals.";
                    break;
                default:
                    html += " by [Unknown grouping style]";
                    break;
            }
            html += "</div>";
            return html;
        }

        #endregion

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!FindAllChildren<RuminantType>().Any())
                yield break;

            if(RuminantGrowActivity is null)
            {
                // check that a grow activity is present for the herd if ruminant types are present.
                string warn = $"[r={Name}] requires at least one [a=RuminantActivityGrow_____] to manage growth and aging of individuals.";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Error);
            }
            else if (FindAllAncestors<IRuminantActivityGrow>().Count() > 1)
            {
                // error if more than one
                yield return new ValidationResult("Only one [a=RuminantActivityGrow_____] activity is permitted in the simulation", new string[] { "Ruminant Herd" });
            }
            if (!FindAllInScope<RuminantActivityDeath>().Any())
            {
                // check that a death activity is present for the herd if ruminant types are present.
                string warn = $"[r={Name}] requires at least one [a=RuminantActivityDeath] to manage death and remove individuals that died.{Environment.NewLine}No individuals will be removed from this simulation even if they have beed identified to have died.";
                Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
            }
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
        /// Name of ruminant type
        /// </summary>
        public string RuminantTypeNameToDisplay { get; set; }

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
        public double AverageAdultEquivalent { get { return (TotalAdultEquivalent ?? 0.0) / Convert.ToDouble(Count ?? 1); } }

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
