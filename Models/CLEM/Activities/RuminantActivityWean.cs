using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.CLEM.Groupings;
using Models.Core.Attributes;
using Models.CLEM.Reporting;
using System.Globalization;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant wean activity</summary>
    /// <summary>This activity will wean the herd</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages weaning of suckling ruminant individuals.")]
    [Version(1, 0, 2, "Weaning style added. Allows decision rule (age, weight, or both to be considered.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantWean.htm")]
    public class RuminantActivityWean: CLEMRuminantActivityBase
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Style of weaning rule
        /// </summary>
        [Description("Weaning rule")]
        public WeaningStyle Style { get; set; }

        /// <summary>
        /// Weaning age (months)
        /// </summary>
        [Description("Weaning age (months)")]
        [Required, GreaterThanEqualValue(0)]
        public double WeaningAge { get; set; }

        /// <summary>
        /// Weaning weight (kg)
        /// </summary>
        [Description("Weaning weight (kg)")]
        [Required, GreaterThanEqualValue(0)]
        public double WeaningWeight { get; set; }

        /// <summary>
        /// Name of GrazeFoodStore (paddock) to place weaners (leave blank for general yards)
        /// </summary>
        [Description("Name of GrazeFoodStore (paddock) to place weaners in")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string GrazeFoodStoreName { get; set; }
        
        private string grazeStore; 
        
        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityWean()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);

            // check GrazeFoodStoreExists
            grazeStore = "";
            if (GrazeFoodStoreName != null && !GrazeFoodStoreName.StartsWith("Not specified"))
            {
                grazeStore = GrazeFoodStoreName.Split('.').Last();
            }
            else
            {
                var ah = this.FindInScope<ActivitiesHolder>();
                if (ah.FindAllDescendants<PastureActivityManage>().Count() != 0)
                {
                    Summary.WriteWarning(this, String.Format("Individuals weaned by [a={0}] will be placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until moved and will require feeding while in yards.\r\nSolution: Set the [GrazeFoodStore to place weaners in] located in the properties.", this.Name));
                }
            }
        }

        /// <summary>An event handler to call for all herd management activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalManage")]
        private void OnCLEMAnimalManage(object sender, EventArgs e)
        {
            // Weaning is performed in the Management event to ensure weaned individuals are treated as unweaned for their intake calculations
            // and the mother is considered lactating for lactation energy demands otherwise IsLactating stops as soon as ind.wean() is performed.

            // if wean month
            if (this.TimingOK)
            {
                double labourlimit = this.LabourLimitProportion;
                int weanedCount = 0;
                ResourceRequest labour = ResourceRequestList.Where(a => a.ResourceType == typeof(LabourType)).FirstOrDefault<ResourceRequest>();
                // Perform weaning
                int count = this.CurrentHerd(false).Where(a => a.Weaned == false).Count();
                foreach (var ind in this.CurrentHerd(false).Where(a => a.Weaned == false))
                {
                    bool readyToWean = false;
                    switch (Style)
                    {
                        case WeaningStyle.AgeOrWeight:
                            readyToWean = (ind.Age >= WeaningAge || ind.Weight >= WeaningWeight);
                            break;
                        case WeaningStyle.AgeOnly:
                            readyToWean = (ind.Age >= WeaningAge);
                            break;
                        case WeaningStyle.WeightOnly:
                            readyToWean = (ind.Weight >= WeaningWeight);
                            break;
                    }

                    if (readyToWean)
                    {
                        this.Status = ActivityStatus.Success;
                        string reason = (ind.Age >= WeaningAge)? "Age" : "Weight";
                        ind.Wean(true, reason);
                        ind.Location = grazeStore;
                        weanedCount++;
                        if (ind.Mother != null)
                        {
                            // report conception status changed when offspring weaned.
                            ind.Mother.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Weaned, ind.Mother, Clock.Today));
                        }
                    }

                    // stop if labour limited individuals reached and LabourShortfallAffectsActivity
                    if (weanedCount > Convert.ToInt32(count * labourlimit, CultureInfo.InvariantCulture))
                    {
                        this.Status = ActivityStatus.Partial;
                        break;
                    }
                }

                if(weanedCount > 0)
                {
                    SetStatusSuccess();
                }
                else
                {
                    this.Status = ActivityStatus.NotNeeded;
                }

            }
        }

        /// <summary>
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="requirement">Labour requirement model</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            List<Ruminant> herd = CurrentHerd(false);
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
                case LabourUnitType.perUnit:
                    double numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Weaning", this.PredictedHerdName);
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
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
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
                htmlWriter.Write("\r\n<div class=\"activityentry\">Individuals are weaned at ");
                if (Style == WeaningStyle.AgeOrWeight | Style == WeaningStyle.AgeOnly)
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + WeaningAge.ToString("#0.#") + "</span> months");
                    if (Style == WeaningStyle.AgeOrWeight)
                    {
                        htmlWriter.Write(" or  ");
                    }
                }
                if (Style == WeaningStyle.AgeOrWeight | Style == WeaningStyle.WeightOnly)
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + WeaningWeight.ToString("##0.##") + "</span> kg");
                }
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">Weaned individuals will be placed in ");
                if (GrazeFoodStoreName == null || GrazeFoodStoreName == "")
                {
                    htmlWriter.Write("<span class=\"resourcelink\">Not specified - general yards</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + GrazeFoodStoreName + "</span>");
                }
                htmlWriter.Write("</div>");
                // warn if natural weaning will take place

                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
