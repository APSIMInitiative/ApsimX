using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using StdUnits;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant dry breeder culling activity</summary>
    /// <summary>This activity provides functionality for kulling dry breeders</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity flags dry breeders for sale. It requires a RuminantActivityBuySell to undertake the sales and removal of individuals.")]
    public class RuminantActivitySellDryBreeders : CLEMRuminantActivityBase
    {
        /// <summary>
        /// Minimum conception rate before any selling
        /// </summary>
        [Description("Minimum conception rate before any selling")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumConceptionBeforeSell { get; set; }

        /// <summary>
        /// Number of months since last birth to be considered dry
        /// </summary>
        [Description("Number of months since last birth to be considered dry")]
        [Required, GreaterThanEqualValue(0)]
        public double MonthsSinceBirth { get; set; }

        /// <summary>
        /// Proportion of dry breeder to sell
        /// </summary>
        [Description("Proportion of dry breeders to sell")]
        [Required, Proportion]
        public double ProportionToRemove { get; set; }

        private List<LabourFilterGroupSpecified> labour { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(false, false);

            // get labour specifications
            labour = Apsim.Children(this, typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (labour == null) labour = new List<LabourFilterGroupSpecified>();
        }

        /// <summary>An event handler to perform herd dry breeder cull</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalMilking")]
        private void OnCLEMAnimalMilking(object sender, EventArgs e)
        {
            if (this.TimingOK & this.Status != ActivityStatus.Ignored)
            {
                if (ProportionToRemove > 0)
                {
                    // get labour shortfall
                    double labourLimiter = 1.0;
                    if(this.Status == ActivityStatus.Partial & this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
                    {
                        double labourLimit = 1;
                        double labourNeeded = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Required);
                        double labourProvided = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Provided);
                        if (labourNeeded > 0)
                        {
                            labourLimit = labourProvided / labourNeeded;
                        }
                    }

                    // get dry breeders
                    RuminantHerd ruminantHerd = Resources.RuminantHerd();
                    List<RuminantFemale> herd = this.CurrentHerd(true).Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().ToList();

                    // get dry breeders from females
                    foreach (RuminantFemale female in herd.Where(a => a.Age - a.AgeAtLastBirth >= MonthsSinceBirth & a.PreviousConceptionRate >= MinimumConceptionBeforeSell & a.AgeAtLastBirth > 0))
                    {
                        if (ZoneCLEM.RandomGenerator.NextDouble() <= ProportionToRemove * labourLimiter)
                        {
                            // flag female ready to transport.
                            female.SaleFlag = HerdChangeReason.DryBreederSale;
                        }
                    }

                }
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            ResourceRequestList = null;
            if (this.TimingOK)
            {
                // get all potential dry breeders
                List<RuminantFemale> herd = this.CurrentHerd(false).Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().Where(a => a.Age - a.AgeAtLastBirth >= MonthsSinceBirth & a.PreviousConceptionRate >= MinimumConceptionBeforeSell & a.AgeAtLastBirth > 0).ToList();
                int head = herd.Count();
                double AE = herd.Sum(a => a.AdultEquivalent);

                if (head == 0) return null;

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
                            daysNeeded = Math.Ceiling(AE / item.UnitSize) * item.LabourPerUnit;
                            break;
                        default:
                            throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
                    }
                    if (daysNeeded > 0)
                    {
                        if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
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
            }
            return ResourceRequestList;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return; ;
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }



    }
}
