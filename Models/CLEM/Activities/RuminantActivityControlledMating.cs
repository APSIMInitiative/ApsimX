using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
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
    public class RuminantActivityControlledMating : CLEMRuminantActivityBase
    {
        [Link]
        private List<LabourRequirement> labour;

        private RuminantGroup breederGroup;

        List<SetAttributeWithValue> attributeList;

        /// <summary>
        /// The available attributes for the breeding sires
        /// </summary>
        public List<SetAttributeWithValue> SireAttributes => attributeList;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.AllocationStyle = ResourceAllocationStyle.Manual;

            this.InitialiseHerd(false, true);

            breederGroup = new RuminantGroup();
            breederGroup.Children.Add(new RuminantFilter() { Parameter = RuminantFilterParameters.Gender, Operator = FilterOperators.Equal, Value="Female" });
            breederGroup.Children.Add(new RuminantFilter() { Parameter = RuminantFilterParameters.IsBreeder, Operator = FilterOperators.Equal, Value = "True" });

            attributeList = this.FindAllDescendants<SetAttributeWithValue>().ToList();

            // get labour specifications
            labour = this.FindAllChildren<LabourRequirement>().Cast<LabourRequirement>().ToList();
            if (labour.Count() == 0)
            {
                labour = new List<LabourRequirement>();
            }

            // check that timer exists for controlled mating
            if (!this.TimingExists)
            {
                Summary.WriteWarning(this, $"Breeding with controlled mating [a={this.Parent.Name}].[a={this.Name}] requires a Timer otherwise breeding will be undertaken every time-step");
            }
        }

        /// <summary>
        /// Provide the list of breeders to mate accounting for the controlled mating failure rate, and required resources
        /// </summary>
        /// <returns>A list of breeders for the breeding activity to work with</returns>
        public IEnumerable<Ruminant> BreedersToMate()
        {
            this.Status = ActivityStatus.NotNeeded;
            if(this.TimingOK) // general Timer or TimeBreedForMilking ok
            {
                // get list of breeders using filtergroups to this activity
                IEnumerable<Ruminant> herd = CurrentHerd(true).FilterRuminants(breederGroup);

                if (herd.Count() > 0)
                {
                    // reduce to breed for milking number
                    // get number needed

                    // calculate labour and finance costs
                    List<ResourceRequest> resourcesneeded = GetResourcesNeededForActivityLocal(herd);
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

                    if (limiter == 1)
                    {
                        this.Status = ActivityStatus.Success;
                    }

                    // report that this activity was performed as it does not use base GetResourcesRequired
                    this.TriggerOnActivityPerformed();

                    return herd.Take(Convert.ToInt32(Math.Floor(herd.Count() * limiter), CultureInfo.InvariantCulture));
                }
                // report that this activity was performed as it does not use base GetResourcesRequired
                this.TriggerOnActivityPerformed();
            }
            return null;
        }

        /// <summary>
        /// Private method to determine resources required for this activity in the current month
        /// This method is local to this activity and not called with CLEMGetResourcesRequired event
        /// </summary>
        /// <returns>List of required resource requests</returns>
        private List<ResourceRequest> GetResourcesNeededForActivityLocal(IEnumerable<Ruminant> breederList)
        {
            ResourceRequestList = null;
            int head = breederList.Count();
            double adultEquivalents = breederList.Sum(a => a.AdultEquivalent);

            if (head == 0)
            {
                return null;
            }

            // get all fees for breeding
            foreach (RuminantActivityFee item in this.FindAllChildren<RuminantActivityFee>())
            {
                if (ResourceRequestList == null)
                {
                    ResourceRequestList = new List<ResourceRequest>();
                }

                double sumneeded = 0;
                switch (item.PaymentStyle)
                {
                    case AnimalPaymentStyleType.Fixed:
                        sumneeded = item.Amount;
                        break;
                    case AnimalPaymentStyleType.perHead:
                        sumneeded = head * item.Amount;
                        break;
                    case AnimalPaymentStyleType.perAE:
                        sumneeded = adultEquivalents * item.Amount;
                        break;
                    default:
                        throw new Exception(String.Format("PaymentStyle ({0}) is not supported for ({1}) in ({2})", item.PaymentStyle, item.Name, this.Name));
                }
                ResourceRequestList.Add(new ResourceRequest()
                {
                    AllowTransmutation = false,
                    Required = sumneeded,
                    ResourceType = typeof(Finance),
                    ResourceTypeName = item.BankAccountName.Split('.').Last(),
                    ActivityModel = this,
                    FilterDetails = null,
                    Category = item.Name
                }
                );
            }

            // for each labour item specified
            foreach (var item in labour)
            {
                double daysNeeded = 0;
                switch (item.UnitType)
                {
                    case LabourUnitType.Fixed:
                        daysNeeded = item.LabourPerUnit;
                        break;
                    case LabourUnitType.perHead:
                        daysNeeded = Math.Ceiling(head / item.UnitSize) * item.LabourPerUnit;
                        break;
                    case LabourUnitType.perAE:
                        daysNeeded = Math.Ceiling(adultEquivalents / item.UnitSize) * item.LabourPerUnit;
                        break;
                    default:
                        throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
                }
                if (daysNeeded > 0)
                {
                    if (ResourceRequestList == null)
                    {
                        ResourceRequestList = new List<ResourceRequest>();
                    }

                    ResourceRequestList.Add(new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = daysNeeded,
                        ResourceType = typeof(Labour),
                        ResourceTypeName = "",
                        ActivityModel = this,
                        FilterDetails = new List<object>() { item }
                    }
                    );
                }
            }
            return ResourceRequestList;
        }

        /// <inheritdoc/>
        public override void AdjustResourcesNeededForActivity()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void DoActivity()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                // set attribute with value
                IEnumerable<SetAttributeWithValue> attributeSetters = this.FindAllChildren<SetAttributeWithValue>();
                if (attributeSetters.Count() > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("This activity provides the Attributes of the sire to ensure inheritance to offpsring");
                    htmlWriter.Write("</div>");
                }
                return htmlWriter.ToString();
            }
        }
        #endregion


    }
}
