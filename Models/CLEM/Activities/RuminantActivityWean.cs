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
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manages weaning of suckling ruminant individuals based on age and/or weight")]
    [Version(1, 0, 2, "Weaning style added. Allows decision rule (age, weight, or both to be considered.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantWean.htm")]
    public class RuminantActivityWean: CLEMRuminantActivityBase
    {
        [Link]
        private Clock clock = null;

        private string grazeStore;

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
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Not specified - general yards", typeof(GrazeFoodStore) } })]
        [System.ComponentModel.DefaultValue("Not specified - general yards")]
        public string GrazeFoodStoreName { get; set; }
      
        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityWean()
        {
            this.SetDefaults();
            TransactionCategory = "Livestock.Manage";
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
                    Summary.WriteMessage(this, String.Format("Individuals weaned by [a={0}] will be placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until moved and will require feeding while in yards.\r\nSolution: Set the [GrazeFoodStore to place weaners in] located in the properties.", this.Name), MessageType.Warning);
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
                    string reason = "";
                    switch (Style)
                    {
                        case WeaningStyle.AgeOrWeight:
                            readyToWean = (ind.Age >= WeaningAge || ind.Weight >= WeaningWeight);
                            reason = (ind.Age >= WeaningAge) ? ((ind.Weight >= WeaningWeight) ? "AgeAndWeight": "Age") : "Weight";
                            break;
                        case WeaningStyle.AgeOnly:
                            readyToWean = (ind.Age >= WeaningAge);
                            reason = "Age";
                            break;
                        case WeaningStyle.WeightOnly:
                            readyToWean = (ind.Weight >= WeaningWeight);
                            reason = "Weight";
                            break;
                    }

                    if (readyToWean)
                    {
                        this.Status = ActivityStatus.Success;
                        ind.Wean(true, reason);
                        ind.Location = grazeStore;
                        weanedCount++;

                        // report wean. If mother has died create temp female with the mother's ID for reporting only
                        ind.BreedParams.OnConceptionStatusChanged(new Reporting.ConceptionStatusChangedEventArgs(Reporting.ConceptionStatus.Weaned, ind.Mother?? new RuminantFemale(ind.BreedParams, -1, 999) { ID = ind.MotherID }, clock.Today, ind));
                    }

                    // stop if labour limited individuals reached and LabourShortfallAffectsActivity
                    if (weanedCount > Convert.ToInt32(count * labourlimit, CultureInfo.InvariantCulture))
                    {
                        this.Status = ActivityStatus.Partial;
                        break;
                    }
                }

                if(weanedCount > 0)
                    SetStatusSuccess();
                else
                    this.Status = ActivityStatus.NotNeeded;
            }
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
                case LabourUnitType.perUnit:
                    double numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                        numberUnits = Math.Ceiling(numberUnits);

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
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
                htmlWriter.Write("\r\n<div class=\"activityentry\">Individuals are weaned at ");
                if (Style == WeaningStyle.AgeOrWeight | Style == WeaningStyle.AgeOnly)
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + WeaningAge.ToString("#0.#") + "</span> months");
                    if (Style == WeaningStyle.AgeOrWeight)
                        htmlWriter.Write(" or  ");
                }
                if (Style == WeaningStyle.AgeOrWeight | Style == WeaningStyle.WeightOnly)
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + WeaningWeight.ToString("##0.##") + "</span> kg");
                }
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">Weaned individuals will be placed in ");
                if (GrazeFoodStoreName == null || GrazeFoodStoreName == "")
                    htmlWriter.Write("<span class=\"resourcelink\">Not specified - general yards</span>");
                else
                    htmlWriter.Write("<span class=\"resourcelink\">" + GrazeFoodStoreName + "</span>");
                htmlWriter.Write("</div>");
                // warn if natural weaning will take place
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
