using Models.CLEM.Groupings;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Adds artificial insemination to Ruminant breeding
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityBreed))]
    [Description("Adds controlled mating details to ruminant breeding")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantControlledMating.htm")]
    [Version(1, 0, 1, "")]
    public class RuminantActivityControlledMating : CLEMRuminantActivityBase, IValidatableObject, ICanHandleIdentifiableChildModels

    {
        private List<SetAttributeWithValue> attributeList;
        private ActivityTimerBreedForMilking milkingTimer;
        private RuminantActivityBreed breedingParent;

        private int numberToDo;
        private int numberToSkip;
        private int amountToSkip;
        private int amountToDo;
        private IEnumerable<RuminantFemale> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroups;

        /// <summary>
        /// Maximum age for mating (months)
        /// </summary>
        [Description("Maximum female age for mating")]
        [Category("General", "All")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(120)]
        public double MaximumAgeMating { get; set; }

        /// <summary>
        /// Number joinings per male before male genetics replaced
        /// </summary>
        [Description("Joinings per individual male (genetics)")]
        [Category("Genetics", "All")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1)]
        public int JoiningsPerMale { get; set; }

        /// <summary>
        /// The available attributes for the breeding sires
        /// </summary>
        public List<SetAttributeWithValue> SireAttributes => attributeList;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityControlledMating()
        {
            SetDefaults();
            this.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
            TransactionCategory = "Livestock.Manage";
        }

        /// <inheritdoc/>
        public override LabelsForIdentifiableChildren DefineIdentifiableChildModelLabels<T>()
        {
            switch (typeof(T).Name)
            {
                case "RuminantGroup":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>() {
                            "Breeders to mate" },
                        units: new List<string>()
                        );
                case "RuminantActivityFee":
                case "LabourRequirement":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>() {
                            "Number mated",
                            "Number conceived"
                        },
                        units: new List<string>() {
                            "fixed",
                            "per head",
                        }
                        );
                default:
                    return new LabelsForIdentifiableChildren();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.AllocationStyle = ResourceAllocationStyle.ByParent;
            this.InitialiseHerd(false, true);
            filterGroups = GetIdentifiableChildrenByIdentifier<RuminantGroup>("Females to mate", false, true);

            attributeList = this.FindAllDescendants<SetAttributeWithValue>().ToList();

            milkingTimer = FindChild<ActivityTimerBreedForMilking>();

            // check that timer exists for controlled mating
            if (!this.TimingExists)
                Summary.WriteMessage(this, $"Breeding with controlled mating [a={this.Parent.Name}].[a={this.Name}] requires a Timer otherwise breeding will be undertaken every time-step", MessageType.Warning);

            // get details from parent breeding activity
            breedingParent = this.Parent as RuminantActivityBreed;
        }

        /// <summary>
        /// Provide the list of all breeders currently available
        /// </summary>
        /// <returns>A list of breeders to work with before returning to the breed activity</returns>
        private IEnumerable<RuminantFemale> GetBreeders()
        {
            // return the full list of breeders currently able to breed
            // controlled mating includes a max breeding age property, so reduces numbers mated
            var fullSetBreeders = milkingTimer != null
                ? milkingTimer.IndividualsToBreed
                : CurrentHerd(true).OfType<RuminantFemale>()
                    .Where(a => a.IsAbleToBreed & a.Age <= MaximumAgeMating);


            return fullSetBreeders;
        }

        /// <inheritdoc/>
        protected override void OnGetResourcesPerformActivity(object sender, EventArgs e)
        {
            // this needs to be called by parent by requesting breeders instead ManageActivityResourcesAndTasks();
        }


        /// <inheritdoc/>
        protected override List<ResourceRequest> DetermineResourcesForActivity()
        {
            amountToDo = 0;
            amountToSkip = 0;
            numberToDo = 0;
            numberToSkip = 0;
            IEnumerable<RuminantFemale> herd = GetBreeders();
            uniqueIndividuals = GetUniqueIndividuals<RuminantFemale>(filterGroups, herd);
            numberToDo = uniqueIndividuals?.Count() ?? 0;

            // provide updated units of measure for identifiable children
            foreach (var valueToSupply in valuesForIdentifiableModels.ToList())
            {
                int number = numberToDo;
                switch (valueToSupply.Key.identifier)
                {
                    case "Number mated":
                        switch (valueToSupply.Key.unit)
                        {
                            case "fixed":
                                valuesForIdentifiableModels[valueToSupply.Key] = 1;
                                break;
                            case "per head":
                                valuesForIdentifiableModels[valueToSupply.Key] = number;
                                break;
                            default:
                                throw new NotImplementedException($"Unknown units [{((valueToSupply.Key.unit=="")?"Blank":valueToSupply.Key.unit)}] for [{valueToSupply.Key.identifier}] identifier in [a={NameWithParent}]");
                        }
                        break;
                    case "Number conceived":
                        switch (valueToSupply.Key.unit)
                        {
                            case "fixed":
                                valuesForIdentifiableModels[valueToSupply.Key] = 1;
                                break;
                            case "per head":
                                // need to estimate conception rate
                                foreach (RuminantFemale female in uniqueIndividuals)
                                {
                                    if (female.BreedParams.ConceptionModel == null)
                                        throw new ApsimXException(this, $"No conception details were found for [r={female.BreedParams.Name}]\r\nPlease add a conception component below the [r=RuminantType]");
                                    double rate = female.BreedParams.ConceptionModel.ConceptionRate(female);
                                    if (RandomNumberGenerator.Generator.NextDouble() <= rate)
                                    {
                                        female.ActivityDeterminedConceptionRate = rate;
                                        amountToDo++;
                                    }
                                    else
                                    {
                                        female.ActivityDeterminedConceptionRate = 0;
                                    }
                                }
                                valuesForIdentifiableModels[valueToSupply.Key] = amountToDo;
                                break;
                            default:
                                throw new NotImplementedException($"Unknown units [{((valueToSupply.Key.unit == "") ? "Blank" : valueToSupply.Key.unit)}] for [{valueToSupply.Key.identifier}] identifier in [a={NameWithParent}]");
                        }
                        break;
                    default:
                        throw new NotImplementedException($"Unknown identifier [{((valueToSupply.Key.unit == "") ? "Blank" : valueToSupply.Key.unit)}] used in [a={NameWithParent}]");
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForActivity()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var numberShort = shortfalls.Where(a => a.IdentifiableChildDetails.identifier == "Number mated").FirstOrDefault();
                if (numberShort != null)
                    numberToSkip = Convert.ToInt32(numberToDo * numberShort.Required / numberShort.Provided);

                var amountShort = shortfalls.Where(a => a.IdentifiableChildDetails.identifier == "Number conceived").FirstOrDefault();
                if (amountShort != null)
                    amountToSkip = Convert.ToInt32(amountToDo * amountShort.Required / amountShort.Provided);

                this.Status = ActivityStatus.Partial;
            }
        }

        /// <inheritdoc/>
        protected override void PerformTasksForActivity()
        {
            List<RuminantFemale> selectedBreeders = new List<RuminantFemale>();
            if (numberToDo-numberToSkip > 0)
            {
                amountToDo -= amountToSkip;
                int mated = 1;
                selectedBreeders = uniqueIndividuals.ToList();
                foreach (RuminantFemale ruminant in selectedBreeders)
                {
                    // if no more conceptions allowed
                    if (mated <= numberToDo || amountToDo <= 0)
                    {
                        ruminant.ActivityDeterminedConceptionRate = null;
                    }
                    else
                    {
                        mated++;
                        if ((ruminant.ActivityDeterminedConceptionRate ?? -1) > 0)
                            amountToDo--;
                    }
                }

                if (mated == numberToDo && amountToDo <= 0)
                    SetStatusSuccess();
                else
                    this.Status = ActivityStatus.Partial;
            }
            uniqueIndividuals = selectedBreeders;
        }

        /// <summary>
        /// Provide the list of breeders to mate accounting for the controlled mating failure rate, and required resources
        /// </summary>
        /// <returns>A list of breeders for the breeding activity to work with</returns>
        public IEnumerable<RuminantFemale> BreedersToMate()
        {
            // fire all nneded processes to account for resources required
            ManageActivityResourcesAndTasks();
            // return resulting list with conception precalculated back to the breeding activity.
            return uniqueIndividuals;
        }

        ///// <summary>
        ///// Provide the list of breeders to mate accounting for the controlled mating failure rate, and required resources
        ///// </summary>
        ///// <returns>A list of breeders for the breeding activity to work with</returns>
        //public IEnumerable<RuminantFemale> BreedersToMate()
        //{
        //    IEnumerable<RuminantFemale> breeders = null;
        //    this.Status = ActivityStatus.NotNeeded;
        //    if(this.TimingOK) // general Timer or TimeBreedForMilking ok
        //    {
        //        breeders = GetBreeders();
        //        if (breeders != null &&  breeders.Any())
        //        {
        //            // calculate labour and finance costs
        //            List<ResourceRequest> resourcesneeded = GetResourcesNeededForActivityLocal(breeders);
        //            CheckResources(resourcesneeded, Guid.NewGuid());
        //            bool tookRequestedResources = TakeResources(resourcesneeded, true);
        //            // get all shortfalls
        //            double limiter = 1;
        //            if (tookRequestedResources && (ResourceRequestList != null))
        //            {
        //                double cashlimit = 1;
        //                // calculate required and provided for fixed and variable payments
        //                var payments = resourcesneeded.Where(a => a.ResourceType == typeof(Finance)).GroupBy(a => (a.ActivityModel as RuminantActivityFee).Units.ToUpper() == "FIXED").Select(a => new { key = a.Key, required = a.Sum(b => b.Required), provided = a.Sum(b => b.Provided), });
        //                double paymentsRequired = payments.Sum(a => a.required);
        //                double paymentsProvided = payments.Sum(a => a.provided);

        //                double paymentsFixedRequired = payments.Where(a => a.key == true).Sum(a => a.required);

        //                if (paymentsFixedRequired > paymentsProvided)
        //                {
        //                    // not enough finances for fixed payments
        //                    switch (this.OnPartialResourcesAvailableAction)
        //                    {
        //                        case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
        //                            throw new ApsimXException(this, $"There were insufficient [r=Finances] to pay the [Fixed] herd expenses for [{this.Name}]\r\nConsider changing OnPartialResourcesAvailableAction to Skip or Use Partial.");
        //                        case OnPartialResourcesAvailableActionTypes.SkipActivity:
        //                            Status = ActivityStatus.Ignored;
        //                            cashlimit = 0;
        //                            return null;
        //                        case OnPartialResourcesAvailableActionTypes.UseResourcesAvailable:
        //                            Status = ActivityStatus.Warning;
        //                            cashlimit = 0;
        //                            break;
        //                        default:
        //                            break;
        //                    }
        //                }
        //                else
        //                {
        //                    // work out if sufficient money for variable payments 
        //                    double paymentsVariableProvided = paymentsProvided - paymentsFixedRequired;
        //                    if (paymentsVariableProvided < (paymentsRequired - paymentsFixedRequired))
        //                    {
        //                        // not enough finances for variable payments
        //                        switch (this.OnPartialResourcesAvailableAction)
        //                        {
        //                            case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
        //                                throw new ApsimXException(this, $"There were insufficient [r=Finances] to pay the herd expenses for [{this.Name}]\r\nConsider changing OnPartialResourcesAvailableAction to Skip or Use Partial.");
        //                            case OnPartialResourcesAvailableActionTypes.SkipActivity:
        //                                Status = ActivityStatus.Ignored;
        //                                cashlimit = 0;
        //                                return null;
        //                            case OnPartialResourcesAvailableActionTypes.UseResourcesAvailable:
        //                                Status = ActivityStatus.Partial;

        //                                //TODO: calculate true herd serviced based on amount available spread over all fees

        //                                // simply calculates limit as a properotion of the variable costs available
        //                                cashlimit = paymentsVariableProvided / (paymentsRequired - paymentsFixedRequired);
        //                                break;
        //                            default:
        //                                break;
        //                        }
        //                    }
        //                }

        //                double amountLabourNeeded = resourcesneeded.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Required);
        //                double amountLabourProvided = resourcesneeded.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Provided);
        //                double labourlimit = 1;
        //                if (amountLabourNeeded > 0)
        //                {
        //                    labourlimit = amountLabourProvided == 0 ? 0 : amountLabourProvided / amountLabourNeeded;
        //                }

        //                if (labourlimit < 1)
        //                {
        //                    // not enough labour for activity
        //                    switch (this.OnPartialResourcesAvailableAction)
        //                    {
        //                        case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
        //                            throw new ApsimXException(this, $"There were insufficient [r=Labour] for [{this.Name}]\r\nConsider changing OnPartialResourcesAvailableAction to Skip or Use Partial.");
        //                        case OnPartialResourcesAvailableActionTypes.SkipActivity:
        //                            Status = ActivityStatus.Ignored;
        //                            labourlimit = 0;
        //                            return null;
        //                        case OnPartialResourcesAvailableActionTypes.UseResourcesAvailable:
        //                            Status = ActivityStatus.Partial;
        //                            break;
        //                        default:
        //                            break;
        //                    }
        //                }

        //                limiter = Math.Min(cashlimit, labourlimit);
        //            }

        //            if (limiter < 1)
        //                this.Status = ActivityStatus.Partial;
        //            else if (limiter == 1)
        //                this.Status = ActivityStatus.Success;

        //            breeders = breeders.Take(Convert.ToInt32(Math.Floor(breeders.Count() * limiter), CultureInfo.InvariantCulture));
        //        }
        //        // report that this activity was performed as it does not use base GetResourcesRequired
        //        this.TriggerOnActivityPerformed();
        //    }
        //    return breeders;
        //}

        ///// <summary>
        ///// Private method to determine resources required for this activity in the current month
        ///// This method is local to this activity and not called with CLEMGetResourcesRequired event
        ///// </summary>
        ///// <param name="breederList">The breeders being mated</param>
        ///// <returns>List of resource requests</returns>
        //private List<ResourceRequest> GetResourcesNeededForActivityLocal(IEnumerable<Ruminant> breederList)
        //{
        //    return null;
        //}

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (breedingParent is null)
            {
                string[] memberNames = new string[] { "Controlled mating parent" };
                results.Add(new ValidationResult($"Invalid parent component of [a={this.Name}]. Expecting [a=RuminantActivityBreed].[a=RuminantActivityControlledMating]", memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                // set attribute with value
                IEnumerable<SetAttributeWithValue> attributeSetters = this.FindAllChildren<SetAttributeWithValue>();
                if (attributeSetters.Any())
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write($"The Attributes of the sire are {(attributeSetters.Any()? "specified below" : "selected at random ofrm the herd")} to ensure inheritance to offpsring");
                    htmlWriter.Write("</div>");
                }
                else
                {
                    // need to check for mandatory attributes
                    var mandatoryAttributes = this.FindAncestor<Zone>().FindAllDescendants<SetAttributeWithValue>().Where(a => a.Mandatory).Select(a => a.AttributeName).Distinct();
                    if (mandatoryAttributes.Any())
                    {
                        htmlWriter.Write("\r\n<div class=\"activityentry\">");
                        htmlWriter.Write($"The mandatory attributes <span class=\"setvalue\">{string.Join("</span>,<span class=\"setvalue\">", mandatoryAttributes)}</span> required from the breeding males will be randomally selected from the herd");
                        htmlWriter.Write("</div>");
                    }
                }
                return htmlWriter.ToString();
            }
        }
        #endregion


    }
}
