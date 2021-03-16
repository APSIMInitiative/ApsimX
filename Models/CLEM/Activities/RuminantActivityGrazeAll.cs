using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Models.CLEM.Groupings;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant graze activity</summary>
    /// <summary>This activity determines how a ruminant group will graze</summary>
    /// <summary>It is designed to request food via a food store arbitrator</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs grazing of all herds and pastures (paddocks) in the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantGraze.htm")]
    public class RuminantActivityGrazeAll : CLEMRuminantActivityBase
    {
        [Link]
        private Clock Clock = null;

        /// <summary>
        /// Number of hours grazed
        /// Based on 8 hour grazing days
        /// Could be modified to account for rain/heat walking to water etc.
        /// </summary>
        [Description("Number of hours grazed")]
        [Required, Range(0, 8, ErrorMessage = "Value based on maximum 8 hour grazing day"), GreaterThanValue(0)]
        public double HoursGrazed { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            if (Resources.GrazeFoodStore() != null)
            {
                this.InitialiseHerd(true, true);
                // create activity for each pasture type (and common land) and breed at startup
                // do not include common land pasture..
                foreach (GrazeFoodStoreType pastureType in Resources.GrazeFoodStore().Children.Where(a => a.GetType() == typeof(GrazeFoodStoreType) || a.GetType() == typeof(CommonLandFoodStoreType)))
                {
                    RuminantActivityGrazePasture ragp = new RuminantActivityGrazePasture
                    {
                        GrazeFoodStoreModel = pastureType,
                        Clock = Clock,
                        Parent = this,
                        Name = "Graze_" + (pastureType as Model).Name,
                        OnPartialResourcesAvailableAction = this.OnPartialResourcesAvailableAction
                    };
                    ragp.ActivityPerformed += BubblePaddock_ActivityPerformed;
                    ragp.Resources = this.Resources;
                    ragp.InitialiseHerd(true, true);

                    foreach (RuminantType herdType in Resources.RuminantHerd().FindAllChildren<RuminantType>())
                    {
                        RuminantActivityGrazePastureHerd ragpb = new RuminantActivityGrazePastureHerd
                        {
                            GrazeFoodStoreModel = pastureType,
                            RuminantTypeModel = herdType,
                            HoursGrazed = HoursGrazed,
                            Parent = ragp,
                            Name = ragp.Name + "_" + herdType.Name,
                            OnPartialResourcesAvailableAction = this.OnPartialResourcesAvailableAction
                        };
                        if (ragpb.Resources == null)
                        {
                            ragpb.Resources = this.Resources;
                        }
                        if (ragpb.Clock == null)
                        {
                            ragpb.Clock = this.Clock;
                        }
                        ragpb.InitialiseHerd(true, true);
                        if (ragp.ActivityList == null)
                        {
                            ragp.ActivityList = new List<CLEMActivityBase>();
                        }
                        ragp.ActivityList.Add(ragpb);
                        ragpb.ResourceShortfallOccurred += GrazeAll_ResourceShortfallOccurred;
                        ragpb.ActivityPerformed += BubblePaddock_ActivityPerformed;
                    }
                    if (ActivityList == null)
                    {
                        ActivityList = new List<CLEMActivityBase>();
                    }
                    ActivityList.Add(ragp);

                }
            }
            else
            {
                Summary.WriteWarning(this, "No GrazeFoodStore is available for the ruminant grazing activity!");
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (ActivityList != null)
            {
                foreach (RuminantActivityGrazePasture pastureGraze in ActivityList)
                {
                    foreach (RuminantActivityGrazePastureHerd pastureHerd in pastureGraze.ActivityList)
                    {
                        pastureHerd.ResourceShortfallOccurred -= GrazeAll_ResourceShortfallOccurred;
                        pastureHerd.ActivityPerformed -= BubblePaddock_ActivityPerformed;
                    }
                }
            }
        }

        private void GrazeAll_ResourceShortfallOccurred(object sender, EventArgs e)
        {
            // bubble shortfall to Activity base for reporting
            OnShortfallOccurred(e);
        }

        private void BubblePaddock_ActivityPerformed(object sender, EventArgs e)
        {
            ActivityPerformed?.Invoke(sender, e);
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
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="requirement">Labour requirement model</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            List<Ruminant> herd = this.CurrentHerd(false).Where(a => a.Location != "").ToList();

            int head = herd.Count();
            double adultEquivalents = herd.Sum(a => a.AdultEquivalent);
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
                    numberUnits = adultEquivalents / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Graze", this.PredictedHerdName);
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            if(Status != ActivityStatus.Partial && Status != ActivityStatus.Critical)
            {
                Status = ActivityStatus.NoTask;
            }
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

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">All individuals in managed pastures will graze for ");
                if (HoursGrazed <= 0)
                {
                    htmlWriter.Write("<span class=\"errorlink\">" + HoursGrazed.ToString("0.#") + "</span> hours of ");
                }
                else
                {
                    htmlWriter.Write(((HoursGrazed == 8) ? "" : "<span class=\"setvalue\">" + HoursGrazed.ToString("0.#") + "</span> hours of "));
                }

                htmlWriter.Write("the maximum 8 hours each day</span>");
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
