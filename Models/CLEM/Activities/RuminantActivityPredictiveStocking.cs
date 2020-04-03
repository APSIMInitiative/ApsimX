using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.CLEM.Groupings;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant predictive stocking activity</summary>
    /// <summary>This activity ensures the total herd size is acceptible to graze the dry season pasture</summary>
    /// <summary>It is designed to consider individuals already marked for sale and add additional individuals before transport and sale.</summary>
    /// <summary>It will check all paddocks that the specified herd are grazing</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages ruminant stocking during the dry season based upon wet season pasture biomass. It requires a RuminantActivityBuySell to undertake the sales and removal of individuals.")]
    [Version(1, 0, 3, "Avoids double accounting while removing individuals")]
    [Version(1, 0, 1, "")]
    [Version(1, 0, 2, "Updated assessment calculations and ability to report results")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantPredictiveStocking.htm")]
    public class RuminantActivityPredictiveStocking: CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Month for assessing dry season feed requirements
        /// </summary>
        [Description("Month for assessing dry season feed requirements (1-12)")]
        [Required, Month]
        public int AssessmentMonth { get; set; }

        /// <summary>
        /// Number of months to assess
        /// </summary>
        [Description("Number of months to assess")]
        [Required, GreaterThanEqualValue(0)]
        public int DrySeasonLength { get; set; }

        /// <summary>
        /// Minimum estimated feed (kg/ha) allowed at end of period
        /// </summary>
        [Description("Minimum estimated feed (kg/ha) allowed at end of period")]
        [Required, GreaterThanEqualValue(0)]
        public double FeedLowLimit { get; set; }

        /// <summary>
        /// Predicted pasture at end of assessment period
        /// </summary>
        public double PasturePredicted { get; private set; }

        /// <summary>
        /// Predicted pasture shortfall at end of assessment period
        /// </summary>
        public double PastureShortfall { get {return Math.Max(0, FeedLowLimit - PasturePredicted); } }

        /// <summary>
        /// AE to destock
        /// </summary>
        public double AeToDestock { get; private set; }

        /// <summary>
        /// AE destocked
        /// </summary>
        public double AeDestocked { get; private set; }

        /// <summary>
        /// AE destock shortfall
        /// </summary>
        public double AeShortfall { get {return AeToDestock - AeDestocked; } }

        /// <summary>
        /// Validate this model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // check that this model contains children RuminantDestockGroups with filters
            var results = new List<ValidationResult>();
            // check that this activity contains at least one RuminantDestockGroups group with filters
            bool destockGroupFound = false;
            foreach (RuminantDestockGroup item in this.Children.Where(a => a.GetType() == typeof(RuminantDestockGroup)))
            {
                foreach (RuminantFilter filter in item.Children.Where(a => a.GetType() == typeof(RuminantFilter)))
                {
                    destockGroupFound = true;
                    break;
                }
                if (destockGroupFound)
                {
                    break;
                }
            }

            if (!destockGroupFound)
            {
                string[] memberNames = new string[] { "Ruminant destocking group" };
                results.Add(new ValidationResult("At least one RuminantDestockGroup with RuminantFilter must be present under this RuminantActivityPredictiveStocking activity", memberNames));
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            AeToDestock = 0;
            AeDestocked = 0;
            this.InitialiseHerd(false, true);
        }

        /// <summary>An event handler to call for changing stocking based on prediced pasture biomass</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalStock")]
        private void OnCLEMAnimalStock(object sender, EventArgs e)
        {
            // this event happens after management has marked individuals for purchase or sale.
            if (Clock.Today.Month == AssessmentMonth)
            {
                this.Status = ActivityStatus.NotNeeded;
                // calculate dry season pasture available for each managed paddock holding stock not flagged for sale
                RuminantHerd ruminantHerd = Resources.RuminantHerd();
                foreach (var paddockGroup in ruminantHerd.Herd.Where(a => a.Location != "").GroupBy(a => a.Location))
                {
                    // multiple breeds are currently not supported as we need to work out what to do with diferent AEs
                    if(paddockGroup.GroupBy(a => a.Breed).Count() > 1)
                    {
                        throw new ApsimXException(this, "Seasonal destocking paddocks containing multiple breeds is currently not supported\nActivity:"+this.Name+", Paddock: "+paddockGroup.Key);
                    }

                    // total adult equivalents not marked for sale of all breeds on pasture for utilisation
                    double totalAE = paddockGroup.Where(a => a.SaleFlag == HerdChangeReason.None).Sum(a => a.AdultEquivalent);

                    double shortfallAE = 0;
                    // Determine total feed requirements for dry season for all ruminants on the pasture
                    // We assume that all ruminant have the BaseAnimalEquivalent to the specified herd
                    GrazeFoodStoreType pasture = Resources.GetResourceItem(this, typeof(GrazeFoodStore), paddockGroup.Key, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
                    double pastureBiomass = pasture.Amount;

                    // Adjust fodder balance for detachment rate (6%/month in NABSA, user defined in CLEM, 3%)
                    // AL found the best estimate for AAsh Barkly example was 2/3 difference between detachment and carryover detachment rate with average 12month pool ranging from 10 to 96% and average 46% of total pasture.
                    double detachrate = pasture.DetachRate + ((pasture.CarryoverDetachRate - pasture.DetachRate) * 0.66);
                    // Assume a consumption rate of 2% of body weight.
                    double feedRequiredAE = paddockGroup.FirstOrDefault().BreedParams.BaseAnimalEquivalent * 0.02 * 30.4; //  2% of AE animal per day
                    for (int i = 0; i <= this.DrySeasonLength; i++)
                    {
                        // only include detachemnt if current biomass is positive, not already overeaten
                        if (pastureBiomass > 0)
                        {
                            pastureBiomass *= (1.0 - detachrate);
                        }
                        if (i > 0) // not in current month as already consumed by this time.
                        {
                            pastureBiomass -= (feedRequiredAE * totalAE);
                        }
                    }

                    // Shortfall in Fodder in kg per hectare
                    // pasture at end of period in kg/ha
                    double pastureShortFallKgHa = pastureBiomass / pasture.Manager.Area;
                    PasturePredicted = pastureShortFallKgHa;
                    // shortfall from low limit
                    pastureShortFallKgHa = Math.Max(0, FeedLowLimit - pastureShortFallKgHa);
                    // Shortfall in Fodder in kg for paddock
                    double pastureShortFallKg = pastureShortFallKgHa * pasture.Manager.Area;

                    if (pastureShortFallKg == 0)
                    {
                        return;
                    }

                    // number of AE to sell to balance shortfall_kg over entire season
                    shortfallAE = pastureShortFallKg / (feedRequiredAE* this.DrySeasonLength);
                    AeToDestock = shortfallAE;

                    // get prediction
                    HandleDestocking(shortfallAE, paddockGroup.Key);

                    // fire event to allow reporting of findings
                    OnReportStatus(new EventArgs());
                }
            }
            else
            {
                this.Status = ActivityStatus.Ignored;
            }
        }

        private void HandleDestocking(double animalEquivalentsforSale, string paddockName)
        {
            if (animalEquivalentsforSale <= 0)
            {
                AeDestocked = 0;
                this.Status = ActivityStatus.Ignored;
                return;
            }

            // move to underutilised paddocks
            // TODO: This can be added later as an activity including spelling

            // remove all potential purchases from list as they can't be supported.
            // This does not change the shortfall AE as they were not counted in TotalAE pressure.
            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            ruminantHerd.PurchaseIndividuals.RemoveAll(a => a.Location == paddockName);

            // remove individuals to sale as specified by destock groups
            foreach (RuminantDestockGroup item in this.Children.Where(a => a.GetType() == typeof(RuminantDestockGroup)))
            {
                // works with current filtered herd to obey filtering.
                List<Ruminant> herd = this.CurrentHerd(false).Where(a => a.Location == paddockName && !a.ReadyForSale).ToList();
                herd = herd.Filter(item);
                int cnt = 0;
                while (cnt < herd.Count() && animalEquivalentsforSale > 0)
                {
                    this.Status = ActivityStatus.Success;
                    if(herd[cnt].SaleFlag != HerdChangeReason.DestockSale)
                    {
                        animalEquivalentsforSale -= herd[cnt].AdultEquivalent;
                        herd[cnt].SaleFlag = HerdChangeReason.DestockSale;
                    }
                    cnt++;
                }
                if (animalEquivalentsforSale <= 0)
                {
                    AeDestocked = 0;
                    this.Status = ActivityStatus.Success;
                    return;
                }
            }
            AeDestocked = AeToDestock - animalEquivalentsforSale;
            this.Status = ActivityStatus.Partial;

            // Idea of possible destock groups
            // Steers, Male, Not BreedingSire, > Age
            // Dry Cows, IsDryBreeder
            // Breeders, IsBreeder, !IsPregnant, > Age
            // Underweight ProportionOfMaxWeight < 0.6
            
            // handling of sucklings with sold female is in RuminantActivityBuySell

            // buy or sell is handled by the buy sell activity
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
        /// Determine the labour required for this activity based on LabourRequired items in tree
        /// </summary>
        /// <param name="requirement">Labour requirement model</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            throw new NotImplementedException();
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
        /// Activity performed event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Activity occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Report destock status
        /// </summary>
        public event EventHandler ReportStatus;

        /// <summary>
        /// Report status occurred 
        /// </summary>
        /// <param name="e"></param>
        protected void OnReportStatus(EventArgs e)
        {
            ReportStatus?.Invoke(this, e);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">Pasture will be assessed in ";
            if (AssessmentMonth > 0 & AssessmentMonth <= 12)
            {
                html += "<span class=\"setvalue\">";
                html += new DateTime(2000, AssessmentMonth, 1).ToString("MMMM");
            }
            else
            {
                html += "<span class=\"errorlink\">No month set";
            }
            html += "</span> for a dry season of ";
            if (DrySeasonLength > 0)
            {
                html += "<span class=\"setvalue\">";
                html += DrySeasonLength.ToString("#0");
            }
            else
            {
                html += "<span class=\"errorlink\">No length";
            }
            html += "</span> months ";
            html += "</div>";
            html += "\n<div class=\"activityentry\">The herd will be sold to maintain ";
            html += "<span class=\"setvalue\">";
            html += FeedLowLimit.ToString("#,##0");
            html += "</span> kg/ha at the end of this period";
            html += "</div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n</div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activitygroupsborder\">";
            html += "<div class=\"labournote\">Individuals will be sold in the following order</div>";

            if(Apsim.Children(this, typeof(RuminantDestockGroup)).Count() == 0)
            {
                html += "\n<div class=\"errorlink\">No ruminant filter groups provided</div>";
            }
            return html;
        }

    }
}
