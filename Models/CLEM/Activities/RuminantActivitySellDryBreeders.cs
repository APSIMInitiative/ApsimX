using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using StdUnits;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.Core.Attributes;

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
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantDryBreeders.htm")]
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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);
        }

        /// <summary>An event handler to perform herd dry breeder cull</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalMilking")]
        private void OnCLEMAnimalMilking(object sender, EventArgs e)
        {
            if (this.TimingOK && this.Status != ActivityStatus.Ignored)
            {
                if (ProportionToRemove > 0)
                {
                    // get labour shortfall
                    double labourLimiter = 1.0;
                    if(this.Status == ActivityStatus.Partial && this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
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
                    foreach (RuminantFemale female in herd.Where(a => a.Age - a.AgeAtLastBirth >= MonthsSinceBirth && a.PreviousConceptionRate <= MinimumConceptionBeforeSell && a.AgeAtLastBirth > 0))
                    {
                        if (RandomNumberGenerator.Generator.NextDouble() <= ProportionToRemove * labourLimiter)
                        {
                            // flag female ready to transport.
                            female.SaleFlag = HerdChangeReason.DryBreederSale;
                            if (ProportionToRemove * labourLimiter >= 1)
                            {
                                Status = ActivityStatus.Success;
                            }
                            else
                            {
                                Status = ActivityStatus.Partial;
                            }
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
            return null;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            Status = ActivityStatus.NotNeeded;
            return;
        }

        /// <summary>
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="requirement">Labour requirement model</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            // get all potential dry breeders
            List<RuminantFemale> herd = this.CurrentHerd(false).Where(a => a.Gender == Sex.Female).Cast<RuminantFemale>().Where(a => a.Age - a.AgeAtLastBirth >= MonthsSinceBirth && a.PreviousConceptionRate >= MinimumConceptionBeforeSell && a.AgeAtLastBirth > 0).ToList();
            int head = herd.Count();
            double adultEquivalent = herd.Sum(a => a.AdultEquivalent);
            double daysNeeded = 0;
            double numberUnits = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perAE:
                    numberUnits = adultEquivalent / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return daysNeeded;
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
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
            ResourceShortfallOccurred?.Invoke(this, e);
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
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if (ProportionToRemove == 0)
            {
                html += "No dry breeders will be sold";
            }
            else
            {
                html += "<span class=\"setvalue\">" + ProportionToRemove.ToString("0.##%") + "</span> of ";
                html += "dry breeders with a minumum conception rate of ";
                html += "<span class=\"setvalue\">" + MinimumConceptionBeforeSell.ToString("0.###") + "</span> and at least ";
                html += "<span class=\"setvalue\">" + MonthsSinceBirth.ToString() + "</span> months since last birth will be sold";
            }
            return html;
        }
    }
}
