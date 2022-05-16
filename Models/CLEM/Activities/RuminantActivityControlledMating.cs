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
    public class RuminantActivityControlledMating : CLEMRuminantActivityBase, IValidatableObject
    {
        private List<ISetAttribute> attributeList;
        private ActivityTimerBreedForMilking milkingTimer;
        private RuminantActivityBreed breedingParent;

        /// <summary>
        /// Maximum age for mating (months)
        /// </summary>
        [Description("Maximum female age for mating")]
        [Category("General", "All")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(120)]
        public double MaximumAgeMating { get; set; }

        /// <summary>
        /// Number joinings per male
        /// </summary>
        [Description("Number of joinings per male")]
        [Category("Genetics", "All")]
        [Required, GreaterThanValue(0)]
        [System.ComponentModel.DefaultValue(1)]
        public int JoiningsPerMale { get; set; }

        /// <summary>
        /// The available attributes for the breeding sires
        /// </summary>
        public List<ISetAttribute> SireAttributes => attributeList;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityControlledMating()
        {
            SetDefaults();
            this.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
            TransactionCategory = "Livestock.Manage";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.AllocationStyle = ResourceAllocationStyle.Manual;
            this.InitialiseHerd(false, true);

            attributeList = this.FindAllDescendants<ISetAttribute>().ToList();

            milkingTimer = FindChild<ActivityTimerBreedForMilking>();

            // check that timer exists for controlled mating
            if (!this.TimingExists)
                Summary.WriteMessage(this, $"Breeding with controlled mating [a={this.Parent.Name}].[a={this.Name}] requires a Timer otherwise breeding will be undertaken every time-step", MessageType.Warning);

            // get details from parent breeding activity
            breedingParent = this.Parent as RuminantActivityBreed;
        }

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

        /// <summary>An event handler to perfrom actions needed at the start of the time step</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            this.Status = ActivityStatus.NotNeeded;
        }

        /// <summary>
        /// Provide the list of all breeders currently available
        /// </summary>
        /// <returns>A list of breeders to work with before returning to the breed activity</returns>
        private IEnumerable<RuminantFemale> GetBreeders()
        {
            // return the full list of breeders currently able to breed
            // controlled mating includes a max breeding age property, so reduces numbers mated
            return milkingTimer != null
                ? milkingTimer.IndividualsToBreed
                : CurrentHerd(true).OfType<RuminantFemale>()
                    .Where(a => a.IsAbleToBreed & a.Age <= MaximumAgeMating);
        }

        /// <summary>
        /// Provide the list of breeders to mate accounting for the controlled mating failure rate, and required resources
        /// </summary>
        /// <returns>A list of breeders for the breeding activity to work with</returns>
        public IEnumerable<RuminantFemale> BreedersToMate()
        {
            IEnumerable<RuminantFemale> breeders = null;
            this.Status = ActivityStatus.NotNeeded;
            if(this.TimingOK) // general Timer or TimeBreedForMilking ok
            {
                breeders = GetBreeders();
                if (breeders != null &&  breeders.Any())
                {
                    // calculate labour and finance costs
                    List<ResourceRequest> resourcesneeded = GetResourcesNeededForActivityLocal(breeders);
                    CheckResources(resourcesneeded, Guid.NewGuid());
                    bool tookRequestedResources = TakeResources(resourcesneeded, true);
                    // get all shortfalls
                    double limiter = 1;
                    if (tookRequestedResources && (ResourceRequestList != null))
                    {
                        double cashlimit = 1;
                        // calculate required and provided for fixed and variable payments
                        var payments = resourcesneeded.Where(a => a.ResourceType == typeof(Finance)).GroupBy(a => (a.ActivityModel as RuminantActivityFee).PaymentStyle == AnimalPaymentStyleType.Fixed).Select(a => new { key = a.Key, required = a.Sum(b => b.Required), provided = a.Sum(b => b.Provided), });
                        double paymentsRequired = payments.Sum(a => a.required);
                        double paymentsProvided = payments.Sum(a => a.provided);

                        double paymentsFixedRequired = payments.Where(a => a.key == true).Sum(a => a.required);

                        if (paymentsFixedRequired > paymentsProvided)
                        {
                            // not enough finances for fixed payments
                            switch (this.OnPartialResourcesAvailableAction)
                            {
                                case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                                    throw new ApsimXException(this, $"There were insufficient [r=Finances] to pay the [Fixed] herd expenses for [{this.Name}]\r\nConsider changing OnPartialResourcesAvailableAction to Skip or Use Partial.");
                                case OnPartialResourcesAvailableActionTypes.SkipActivity:
                                    Status = ActivityStatus.Ignored;
                                    cashlimit = 0;
                                    return null;
                                case OnPartialResourcesAvailableActionTypes.UseResourcesAvailable:
                                    Status = ActivityStatus.Warning;
                                    cashlimit = 0;
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            // work out if sufficient money for variable payments 
                            double paymentsVariableProvided = paymentsProvided - paymentsFixedRequired;
                            if (paymentsVariableProvided < (paymentsRequired - paymentsFixedRequired))
                            {
                                // not enough finances for variable payments
                                switch (this.OnPartialResourcesAvailableAction)
                                {
                                    case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                                        throw new ApsimXException(this, $"There were insufficient [r=Finances] to pay the herd expenses for [{this.Name}]\r\nConsider changing OnPartialResourcesAvailableAction to Skip or Use Partial.");
                                    case OnPartialResourcesAvailableActionTypes.SkipActivity:
                                        Status = ActivityStatus.Ignored;
                                        cashlimit = 0;
                                        return null;
                                    case OnPartialResourcesAvailableActionTypes.UseResourcesAvailable:
                                        Status = ActivityStatus.Partial;

                                        //TODO: calculate true herd serviced based on amount available spread over all fees

                                        // simply calculates limit as a properotion of the variable costs available
                                        cashlimit = paymentsVariableProvided / (paymentsRequired - paymentsFixedRequired);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }

                        double amountLabourNeeded = resourcesneeded.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Required);
                        double amountLabourProvided = resourcesneeded.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Provided);
                        double labourlimit = 1;
                        if (amountLabourNeeded > 0)
                        {
                            labourlimit = amountLabourProvided == 0 ? 0 : amountLabourProvided / amountLabourNeeded;
                        }

                        if (labourlimit < 1)
                        {
                            // not enough labour for activity
                            switch (this.OnPartialResourcesAvailableAction)
                            {
                                case OnPartialResourcesAvailableActionTypes.ReportErrorAndStop:
                                    throw new ApsimXException(this, $"There were insufficient [r=Labour] for [{this.Name}]\r\nConsider changing OnPartialResourcesAvailableAction to Skip or Use Partial.");
                                case OnPartialResourcesAvailableActionTypes.SkipActivity:
                                    Status = ActivityStatus.Ignored;
                                    labourlimit = 0;
                                    return null;
                                case OnPartialResourcesAvailableActionTypes.UseResourcesAvailable:
                                    Status = ActivityStatus.Partial;
                                    break;
                                default:
                                    break;
                            }
                        }

                        limiter = Math.Min(cashlimit, labourlimit);
                    }

                    if (limiter < 1)
                        this.Status = ActivityStatus.Partial;
                    else if (limiter == 1)
                        this.Status = ActivityStatus.Success;

                    breeders = breeders.Take(Convert.ToInt32(Math.Floor(breeders.Count() * limiter), CultureInfo.InvariantCulture));
                }
                // report that this activity was performed as it does not use base GetResourcesRequired
                this.TriggerOnActivityPerformed();
            }
            return breeders;
        }

        /// <summary>
        /// Private method to determine resources required for this activity in the current month
        /// This method is local to this activity and not called with CLEMGetResourcesRequired event
        /// </summary>
        /// <param name="breederList">The breeders being mated</param>
        /// <returns>List of resource requests</returns>
        private List<ResourceRequest> GetResourcesNeededForActivityLocal(IEnumerable<Ruminant> breederList)
        {
            return null;
        }

        /// <summary>
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="requirement">Labour requirement model</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            IEnumerable<Ruminant> herd = CurrentHerd(false);
            int head = herd.Where(a => a.Weaned == false).Count();

            double daysNeeded = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    daysNeeded = head * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perAE:
                    double sumAE = 0;
                    daysNeeded = sumAE * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, TransactionCategory, this.PredictedHerdName);
        }

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
