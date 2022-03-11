using Models.Core;
using Models.CLEM.Resources;
using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.CLEM.Groupings;
using Models.Core.Attributes;
using System.IO;
using Newtonsoft.Json;
using APSIM.Shared.Utilities;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant predictive stocking activity</summary>
    /// <summary>This activity ensures the total herd size is acceptible to graze the dry season pasture</summary>
    /// <summary>It is designed to consider individuals already marked for sale and add additional individuals before transport and sale.</summary>
    /// <summary>It will check all paddocks that the specified herd are grazing</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manage ruminant stocking during the dry season using predicted future pasture biomass")]
    [Version(1, 1, 0, "Used new event control for activities and allows multi-month decisions.")]
    [Version(1, 0, 3, "Avoids double accounting while removing individuals")]
    [Version(1, 0, 1, "")]
    [Version(1, 0, 2, "Updated assessment calculations and ability to report results")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantPredictiveStocking.htm")]
    public class RuminantActivityPredictiveStocking: CLEMRuminantActivityBase, ICanHandleIdentifiableChildModels
    {
        [Link]
        private Clock clock = null;

        private int numberToSkip = 0;
        private int numberToDo = 0;
        private double amountToSkip = 0;
        private double amountToDo = 0;
        private IEnumerable<Ruminant> uniqueIndividuals;
        private IEnumerable<RuminantGroup> filterGroups = new List<RuminantGroup>();
        private List<(string paddockName, double number, double AE, double AeShortfall)> paddockShortfalls;
        private IEnumerable<GrazeFoodStoreType> paddocks;

        /// <summary>
        /// Last month for assessing dry season feed requirements
        /// </summary>
        [Description("Last month for assessing dry season feed requirements")]
        [Required, Month]
        public MonthsOfYear LastAssessmentMonth { get; set; }

        /// <summary>
        /// Minimum estimated feed (kg/ha) allowed at end of period
        /// </summary>
        [Description("Minimum estimated feed (kg/ha) allowed at end of period")]
        [Required, GreaterThanEqualValue(0)]
        public double FeedLowLimit { get; set; }

        /// <summary>
        /// Predicted pasture at end of assessment period
        /// </summary>
        [JsonIgnore]
        public double PasturePredicted { get; private set; }

        /// <summary>
        /// Predicted pasture shortfall at end of assessment period
        /// </summary>
        public double PastureShortfall { get {return Math.Max(0, FeedLowLimit - PasturePredicted); } }

        /// <summary>
        /// AE to destock
        /// </summary>
        [JsonIgnore]
        public double AeToDestock { get; private set; }

        /// <summary>
        /// AE destocked
        /// </summary>
        [JsonIgnore]
        public double AeDestocked { get; private set; }

        /// <summary>
        /// AE destock shortfall
        /// </summary>
        public double AeShortfall { get {return AeToDestock - AeDestocked; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityPredictiveStocking()
        {
            TransactionCategory = "Livestock.Manage.[Destock]";
        }

        /// <inheritdoc/>
        public override LabelsForIdentifiableChildren DefineIdentifiableChildModelLabels(string type)
        {
            switch (type)
            {
                case "RuminantGroup":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>(),
                        units: new List<string>()
                        );
                case "RuminantActivityFee":
                case "LabourRequirement":
                    return new LabelsForIdentifiableChildren(
                        identifiers: new List<string>() {
                            "Destock required"
                        },
                        units: new List<string>() {
                            "fixed",
                            "per head",
                            "per AE"
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
            AeToDestock = 0;
            AeDestocked = 0;
            this.InitialiseHerd(false, true);
            filterGroups = GetIdentifiableChildrenByIdentifier<RuminantGroup>(true, false);
            paddocks = Resources.FindResourceGroup<GrazeFoodStore>()?.FindAllChildren<GrazeFoodStoreType>();
            paddockShortfalls = new List<(string paddockName, double number, double AE, double AeShortfall)>();
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalStock")]
        protected override void OnGetResourcesPerformActivity(object sender, EventArgs e)
        {
            ManageActivityResourcesAndTasks();
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> DetermineResourcesForActivity(double argument = 0)
        {
            paddockShortfalls.Clear();
            AeToDestock = 0;
            AeDestocked = 0;
            numberToDo = 0;
            numberToSkip = 0;
            amountToDo = 0;
            amountToSkip = 0;
            IEnumerable<Ruminant> herd = GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.NotMarkedForSale).Where(a => (a.Location ?? "") != "");
            uniqueIndividuals = GetUniqueIndividuals<Ruminant>(filterGroups, herd);
            numberToDo = uniqueIndividuals?.Count() ?? 0;

            int monthsToAssess = 0;
            if (clock.Today.Month > (int)LastAssessmentMonth)
                monthsToAssess = 12 - clock.Today.Month + (int)LastAssessmentMonth;
            else
                monthsToAssess  = (int)LastAssessmentMonth - clock.Today.Month;

            foreach (GrazeFoodStoreType pasture in paddocks)
            {
                IEnumerable<Ruminant> paddockIndividuals = uniqueIndividuals.Where(a => a.Location == pasture.Name);

                //// multiple breeds are currently not supported as we need to work out what to do with diferent AEs
                //if (paddockGroup.GroupBy(a => a.Breed).Count() > 1)
                //{
                //    throw new ApsimXException(this, "Seasonal destocking paddocks containing multiple breeds is currently not supported\r\nActivity:" + this.Name + ", Paddock: " + paddockGroup.Key);
                //}

                // total adult equivalents not marked for sale of all breeds on pasture for utilisation
                double totalAE = paddockIndividuals.Sum(a => a.AdultEquivalent);

                double shortfallAE = 0;
                // Determine total feed requirements for dry season for all ruminants on the pasture
                // We assume that all ruminant have the BaseAnimalEquivalent to the specified herd

                double pastureBiomass = pasture.Amount;

                // Adjust fodder balance for detachment rate (6%/month in NABSA, user defined in CLEM, 3%)
                // AL found the best estimate for AAsh Barkly example was 2/3 difference between detachment and carryover detachment rate with average 12month pool ranging from 10 to 96% and average 46% of total pasture.
                double detachrate = pasture.DetachRate + ((pasture.CarryoverDetachRate - pasture.DetachRate) * 0.66);
                // Assume a consumption rate of 2% of body weight.
                double feedRequiredAE = paddockIndividuals.FirstOrDefault().BreedParams.BaseAnimalEquivalent * 0.02 * 30.4; //  2% of AE animal per day
                for (int i = 0; i < monthsToAssess; i++)
                {
                    // only include detachemnt if current biomass is positive, not already overeaten
                    if (MathUtilities.IsPositive(pastureBiomass))
                        pastureBiomass *= (1.0 - detachrate);

                    if (i > 0) // not in current month as already consumed by this time.
                        pastureBiomass -= (feedRequiredAE * totalAE);
                }

                // Shortfall in Fodder in kg per hectare
                // pasture at end of period in kg/ha
                double pastureShortFallKgHa = pastureBiomass / pasture.Manager.Area;
                PasturePredicted = pastureShortFallKgHa;
                // shortfall from low limit
                pastureShortFallKgHa = Math.Max(0, FeedLowLimit - pastureShortFallKgHa);
                // Shortfall in Fodder in kg for paddock
                double pastureShortFallKg = pastureShortFallKgHa * pasture.Manager.Area;

                if (MathUtilities.IsPositive(pastureShortFallKg))
                {
                    // number of AE to sell to balance shortfall_kg over entire season
                    shortfallAE = pastureShortFallKg / (feedRequiredAE * monthsToAssess);
                    AeToDestock += shortfallAE;
                    int number = paddockIndividuals.Count();
                    numberToDo += number;
                    amountToDo += totalAE;
                    paddockShortfalls.Add((pasture.Name, number, totalAE, shortfallAE));
                }
                else
                {
                    paddockShortfalls.Add((pasture.Name, 0, 0, 0));
                }
            }

            // provide updated units of measure for identifiable children
            foreach (var valueToSupply in valuesForIdentifiableModels.ToList())
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForIdentifiableModels[valueToSupply.Key] = 1;
                        break;
                    case "per head":
                        valuesForIdentifiableModels[valueToSupply.Key] = numberToDo;
                        break;
                    case "per AE":
                        valuesForIdentifiableModels[valueToSupply.Key] = amountToDo;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
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
                var numberShort = shortfalls.Where(a => a.IdentifiableChildDetails.identifier == "Destock required" && a.IdentifiableChildDetails.unit != "per AE").FirstOrDefault();
                if (numberShort != null)
                    numberToSkip = Convert.ToInt32(numberToDo * numberShort.Required / numberShort.Provided);

                var amountShort = shortfalls.Where(a => a.IdentifiableChildDetails.identifier == "Destock required" && a.IdentifiableChildDetails.unit == "per AE").FirstOrDefault();
                if (amountShort != null)
                    amountToSkip = Convert.ToInt32(amountToDo * amountShort.Required / amountShort.Provided);

                this.Status = ActivityStatus.Partial;
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForActivity(double argument = 0)
        {
            double amountDone = amountToDo - amountToSkip;
            if (numberToDo - numberToSkip > 0)
            {
                int number = 0;

                // remove all potential purchases from list as they can't be supported.
                // This does not change the shortfall AE as they were not counted in TotalAE pressure.
                foreach (GrazeFoodStoreType pasture in paddocks)
                {
                    HerdResource.PurchaseIndividuals.RemoveAll(a => a.Location == pasture.Name);
                }

                // move to underutilised paddocks
                // TODO: This can be added later as an activity including spelling

                foreach (GrazeFoodStoreType pasture in paddocks)
                {
                    foreach (Ruminant ruminant in uniqueIndividuals.SkipLast(numberToSkip).Where(a => a.Location == pasture.Name).ToList())
                    {
                        if (MathUtilities.IsLessThanOrEqual(amountDone, 0))
                        {
                            amountDone = 0;
                            break;
                        }
                        if (ruminant.SaleFlag != HerdChangeReason.DestockSale)
                        {
                            amountDone -= ruminant.AdultEquivalent;
                            ruminant.SaleFlag = HerdChangeReason.DestockSale;
                            number++;
                        }
                    }
                }

                AeDestocked = amountDone;
                if (number == numberToDo && MathUtilities.FloatsAreEqual(amountDone, amountToDo))
                    SetStatusSuccessOrPartial();
                else
                    this.Status = ActivityStatus.Partial;

                // fire event to allow reporting of findings
                OnReportStatus(new EventArgs());
            }
        }


        ///// <summary>An event handler to call for changing stocking based on prediced pasture biomass</summary>
        ///// <param name="sender">The sender.</param>
        ///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        //[EventSubscribe("CLEMAnimalStock")]
        //private void OnCLEMAnimalStock(object sender, EventArgs e)
        //{
        //    AeToDestock = 0;
        //    AeDestocked = 0;
        //    // this event happens after management has marked individuals for purchase or sale.
        //    if (clock.Today.Month == (int)AssessmentMonth)
        //    {
        //        this.Status = ActivityStatus.NotNeeded;
        //        // calculate dry season pasture available for each managed paddock holding stock not flagged for sale

        //        foreach (var paddockGroup in HerdResource.Herd.Where(a => (a.Location??"") != "").GroupBy(a => a.Location))
        //        {
        //            // multiple breeds are currently not supported as we need to work out what to do with diferent AEs
        //            if(paddockGroup.GroupBy(a => a.Breed).Count() > 1)
        //            {
        //                throw new ApsimXException(this, "Seasonal destocking paddocks containing multiple breeds is currently not supported\r\nActivity:"+this.Name+", Paddock: "+paddockGroup.Key);
        //            }

        //            // total adult equivalents not marked for sale of all breeds on pasture for utilisation
        //            double totalAE = paddockGroup.Where(a => a.SaleFlag == HerdChangeReason.None).Sum(a => a.AdultEquivalent);

        //            double shortfallAE = 0;
        //            // Determine total feed requirements for dry season for all ruminants on the pasture
        //            // We assume that all ruminant have the BaseAnimalEquivalent to the specified herd

        //            GrazeFoodStoreType pasture = Resources.FindResourceType<GrazeFoodStore, GrazeFoodStoreType>(this, paddockGroup.Key, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
        //            double pastureBiomass = pasture.Amount;

        //            // Adjust fodder balance for detachment rate (6%/month in NABSA, user defined in CLEM, 3%)
        //            // AL found the best estimate for AAsh Barkly example was 2/3 difference between detachment and carryover detachment rate with average 12month pool ranging from 10 to 96% and average 46% of total pasture.
        //            double detachrate = pasture.DetachRate + ((pasture.CarryoverDetachRate - pasture.DetachRate) * 0.66);
        //            // Assume a consumption rate of 2% of body weight.
        //            double feedRequiredAE = paddockGroup.FirstOrDefault().BreedParams.BaseAnimalEquivalent * 0.02 * 30.4; //  2% of AE animal per day
        //            for (int i = 0; i <= this.DrySeasonLength; i++)
        //            {
        //                // only include detachemnt if current biomass is positive, not already overeaten
        //                if (MathUtilities.IsPositive(pastureBiomass))
        //                    pastureBiomass *= (1.0 - detachrate);

        //                if (i > 0) // not in current month as already consumed by this time.
        //                    pastureBiomass -= (feedRequiredAE * totalAE);
        //            }

        //            // Shortfall in Fodder in kg per hectare
        //            // pasture at end of period in kg/ha
        //            double pastureShortFallKgHa = pastureBiomass / pasture.Manager.Area;
        //            PasturePredicted = pastureShortFallKgHa;
        //            // shortfall from low limit
        //            pastureShortFallKgHa = Math.Max(0, FeedLowLimit - pastureShortFallKgHa);
        //            // Shortfall in Fodder in kg for paddock
        //            double pastureShortFallKg = pastureShortFallKgHa * pasture.Manager.Area;

        //            if (MathUtilities.FloatsAreEqual(pastureShortFallKg, 0))
        //                return;

        //            // number of AE to sell to balance shortfall_kg over entire season
        //            shortfallAE = pastureShortFallKg / (feedRequiredAE* this.DrySeasonLength);
        //            AeToDestock = shortfallAE;

        //            // get prediction
        //            HandleDestocking(shortfallAE, paddockGroup.Key);

        //            // fire event to allow reporting of findings
        //            OnReportStatus(new EventArgs());
        //        }
        //    }
        //    else
        //        this.Status = ActivityStatus.Ignored;
        //}

        //private void HandleDestocking(double animalEquivalentsforSale, string paddockName)
        //{
        //    if (animalEquivalentsforSale <= 0)
        //    {
        //        AeDestocked = 0;
        //        this.Status = ActivityStatus.Ignored;
        //        return;
        //    }

        //    // move to underutilised paddocks
        //    // TODO: This can be added later as an activity including spelling

        //    // remove all potential purchases from list as they can't be supported.
        //    // This does not change the shortfall AE as they were not counted in TotalAE pressure.
        //    HerdResource.PurchaseIndividuals.RemoveAll(a => a.Location == paddockName);

        //    // remove individuals to sale as specified by destock groups
        //    foreach (var item in FindAllChildren<RuminantGroup>())
        //    {
        //        // works with current filtered herd to obey filtering.
        //        var herd = item.Filter(CurrentHerd(false))
        //            .Where(a => a.Location == paddockName && !a.ReadyForSale);

        //        foreach (Ruminant ruminant in herd)
        //        {
        //            if (ruminant.SaleFlag != HerdChangeReason.DestockSale)
        //            {
        //                animalEquivalentsforSale -= ruminant.AdultEquivalent;
        //                ruminant.SaleFlag = HerdChangeReason.DestockSale;
        //            }

        //            if (MathUtilities.IsLessThanOrEqual(animalEquivalentsforSale, 0))
        //            {
        //                AeDestocked = 0;
        //                this.Status = ActivityStatus.Success;
        //                return;
        //            }
        //        }
        //    }

        //    AeDestocked = AeToDestock - animalEquivalentsforSale;
        //    this.Status = ActivityStatus.Partial;
            
        //    // handling of sucklings with sold female is in RuminantActivityBuySell
        //    // buy or sell is handled by the buy sell activity
        //}

        /// <inheritdoc/>
        public event EventHandler ReportStatus;

        /// <inheritdoc/>
        protected void OnReportStatus(EventArgs e)
        {
            ReportStatus?.Invoke(this, e);
        }

        //#region validation

        ///// <summary>
        ///// Validate this model
        ///// </summary>
        ///// <param name="validationContext"></param>
        ///// <returns></returns>
        //public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        //{
        //    // check that this model contains children RuminantDestockGroups with filters
        //    var results = new List<ValidationResult>();
        //    // check that this activity contains at least one RuminantGroup with Destock reason (filters optional as someone might want to include entire herd)
        //    if (this.FindAllChildren<RuminantGroup>().Count() == 0)
        //    {
        //        string[] memberNames = new string[] { "Ruminant group" };
        //        results.Add(new ValidationResult("At least one [f=RuminantGroup] must be present under this [a=RuminantActivityPredictiveStocking] activity", memberNames));
        //    }
        //    return results;
        //}
        //#endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Pasture will be assessed in months defined by a Timer and assessed until ");
                if ((int)LastAssessmentMonth > 0 & (int)LastAssessmentMonth <= 12)
                {
                    htmlWriter.Write("<span class=\"setvalue\">");
                    htmlWriter.Write(LastAssessmentMonth.ToString());
                }
                else
                    htmlWriter.Write("<span class=\"errorlink\">No month set");

                htmlWriter.Write("</span></div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">The herd will be sold to maintain ");
                htmlWriter.Write("<span class=\"setvalue\">");
                htmlWriter.Write(FeedLowLimit.ToString("#,##0"));
                htmlWriter.Write("</span> kg/ha at the end of this period");
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            return "\r\n</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            string html = "";
            html += "\r\n<div class=\"activitygroupsborder\">";
            html += "<div class=\"labournote\">Individuals will be sold in the following order</div>";

            if (FindAllChildren<RuminantGroup>().Count() == 0)
                html += "\r\n<div class=\"errorlink\">No ruminant filter groups provided</div>";
            return html;
        } 
        #endregion

    }
}
