using APSIM.Shared.Utilities;
using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using System.Globalization;
using Models.CLEM.Groupings;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant predictive stocking activity using ENSO predictions</summary>
    /// <summary>This activity will undertake stocking and destocking based on future season predictions (La Nini or El Nino)</summary>
    /// <summary>It is designed to consider individuals already marked for sale and add additional individuals before transport and sale.</summary>
    /// <summary>It will check all paddocks that the specified herd are grazing</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages ruminant stocking based on predicted seasonal outlooks. It requires a RuminantActivityBuySell to undertake the sales and removal of individuals.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantPredictiveStockingENSO.htm")]
    public class RuminantActivityPredictiveStockingENSO: CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// File containing SOI measure from BOM http://www.bom.gov.au/climate/influences/timeline/
        /// Year Jan Feb Mar.....
        /// 1876 11  0.2 -3  +ve LaNina -ve El Nino
        /// </summary>
        [Description("SOI monthly data file")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "SOI monthly data filename required")]
        [Models.Core.Display(Type = DisplayType.FileName)]
        public string MonthlySOIFile { get; set; }

        /// <summary>
        /// Number of previous months to consider
        /// </summary>
        [Description("Number of months to assess")]
        [Required, GreaterThanValue(0)]
        public int AssessMonths { get; set; }

        /// <summary>
        /// Mean SOI value before considered La Niña
        /// </summary>
        [Description("SOI cutoff before considered La Niña")]
        [Required, GreaterThanEqualValue(0)]
        public double SOIForLaNina { get; set; }

        /// <summary>
        /// Mean  SOI value before considered El Niño
        /// </summary>
        [Description("SOI cutoff (-ve) before considered El Niño")]
        [Required, GreaterThanEqualValue(0)]
        public double SOIForElNino { get; set; }

        private Relationship pastureToStockingChangeElNino { get; set; }
        private Relationship pastureToStockingChangeLaNina { get; set; }

        /// <summary>
        /// Minimum estimated feed (kg/ha) before restocking
        /// </summary>
        [Description("Minimum estimated feed (kg/ha) before restocking")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumFeedBeforeRestock { get; set; }

        /// <summary>
        /// AE to destock
        /// </summary>
        [field: NonSerialized]
        public double AeToDestock { get; private set; }

        /// <summary>
        /// AE destocked
        /// </summary>
        [field: NonSerialized]
        public double AeDestocked { get; private set; }

        /// <summary>
        /// AE destock shortfall
        /// </summary>
        public double AeShortfall { get { return AeToDestock - AeDestocked; } }

        /// <summary>
        /// AE to restock
        /// </summary>
        [field: NonSerialized]
        public double AeToRestock { get; private set; }

        /// <summary>
        /// AE restocked
        /// </summary>
        [field: NonSerialized]
        public double AeRestocked { get; private set; }

        private string fullFilename;
        private Dictionary<DateTime, double> ForecastSequence;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityPredictiveStockingENSO()
        {
            this.SetDefaults();
        }

        #region validation

        /// <summary>
        /// Validate this model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            return results;
        }
        #endregion

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            ForecastSequence = new Dictionary<DateTime, double>();

            Simulation simulation = FindAncestor<Simulation>();
            if (simulation != null)
            {
                fullFilename = PathUtilities.GetAbsolutePath(this.MonthlySOIFile, simulation.FileName);
            }
            else
            {
                fullFilename = this.MonthlySOIFile;
            }

            //check file exists
            if (File.Exists(fullFilename))
            {
                // load ENSO file into memory
                using (StreamReader ensoStream = new StreamReader(MonthlySOIFile))
                {
                    string line = "";
                    while ((line = ensoStream.ReadLine()) != null)
                    {
                        if (!line.StartsWith("Year"))
                        {
                            string[] items = line.Split(' ');
                            for (int i = 1; i < items.Count(); i++)
                            {
                                ForecastSequence.Add(
                                    new DateTime(Convert.ToInt16(items[0]), Convert.ToInt16(i, CultureInfo.InvariantCulture), 1),
                                    Convert.ToDouble(items[i], CultureInfo.InvariantCulture)
                                    );
                            }
                        }
                    }
                }
            }
            else
            { 
                Summary.WriteWarning(this, String.Format("@error:Could not find ENSO-SOI datafile [x={0}[ for [a={1}]", MonthlySOIFile, this.Name));
            }

            this.InitialiseHerd(false, true);

            // try attach relationships
            pastureToStockingChangeElNino = this.FindAllChildren<Relationship>().Where(a => a.Name.ToLower().Contains("nino")).FirstOrDefault();
            pastureToStockingChangeLaNina = this.FindAllChildren<Relationship>().Where(a => a.Name.ToLower().Contains("nina")).FirstOrDefault();
        }

        private ENSOState GetENSOMeasure()
        {
            // gets the average monthly Southern Oscillation Index from last six months
            // won't work for start of record (1876)
            // if average >= 7 assumed La Nina. If <= 7 El Nino, else Neutral
            // http://www.bom.gov.au/climate/influences/timeline/

            DateTime date = new DateTime(Clock.Today.Year, Clock.Today.Month, 1);
            int monthsAvailable = ForecastSequence.Where(a => a.Key >= date && a.Key <= date.AddMonths(-6)).Count();
            // get sum of previous 6 months
            double ensoValue = ForecastSequence.Where(a => a.Key >= date && a.Key <= date.AddMonths(-6)).Sum(a => a.Value);
            // get average SIOIndex
            ensoValue /= monthsAvailable;
            if(ensoValue <= SOIForElNino)
            {
                return ENSOState.ElNino;
            }
            else if (ensoValue >= SOIForLaNina)
            {
                return ENSOState.LaNina;
            }
            else
            {
                return ENSOState.Neutral;
            }
        } 

        /// <summary>An event handler to call for all resources other than food for feeding activity</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalStock")]
        private void OnCLEMAnimalStock(object sender, EventArgs e)
        {
            AeToDestock = 0;
            AeDestocked = 0;
            AeToRestock = 0;
            AeRestocked = 0;

            // this event happens after management has marked individuals for purchase or sale.
            if (this.TimingOK)
            {
                // Get ENSO forcase for current time
                ENSOState forecastEnsoState = GetENSOMeasure();

                this.Status = ActivityStatus.NotNeeded;

                // calculate dry season pasture available for each managed paddock holding stock
                RuminantHerd ruminantHerd = Resources.RuminantHerd();
                foreach (var newgroup in ruminantHerd.Herd.Where(a => a.Location != "").GroupBy(a => a.Location))
                {
                    double aELocationNeeded = 0;

                    // total adult equivalents of all breeds on pasture for utilisation
                    double totalAE = newgroup.Sum(a => a.AdultEquivalent);
                    // determine AE marked for sale and purchase of managed herd
                    double markedForSaleAE = newgroup.Where(a => a.ReadyForSale).Sum(a => a.AdultEquivalent);
                    double purchaseAE = ruminantHerd.PurchaseIndividuals.Where(a => a.Location == newgroup.Key).Sum(a => a.AdultEquivalent);

                    double herdChange = 1.0;
                    bool relationshipFound = false;
                    switch (forecastEnsoState)
                    {
                        case ENSOState.Neutral:
                            break;
                        case ENSOState.ElNino:
                            if (!(pastureToStockingChangeElNino is null))
                            {
                                GrazeFoodStoreType pasture = Resources.GetResourceItem(this, typeof(GrazeFoodStoreType), newgroup.Key, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as GrazeFoodStoreType;
                                double kgha = pasture.TonnesPerHectare * 1000;
                                herdChange = pastureToStockingChangeElNino.SolveY(kgha);
                                relationshipFound = true;
                            }
                            break;
                        case ENSOState.LaNina:
                            if (!(pastureToStockingChangeLaNina is null))
                            {
                                GrazeFoodStoreType pasture = Resources.GetResourceItem(this, typeof(GrazeFoodStoreType), newgroup.Key, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as GrazeFoodStoreType;
                                double kgha = pasture.TonnesPerHectare * 1000;
                                herdChange = pastureToStockingChangeLaNina.SolveY(kgha);
                                relationshipFound = true;
                            }
                            break;
                        default:
                            break;
                    }
                    if (!relationshipFound)
                    {
                        string warn = $"No pasture biomass to herd change proportion [Relationship] provided for {((forecastEnsoState== ENSOState.ElNino)? "El Niño":"La Niña")} phase in [a={this.Name}]\r\nNo stock management will be performed in this phase.";
                        this.Status = ActivityStatus.Warning;
                        if (!Warnings.Exists(warn))
                        {
                            Summary.WriteWarning(this, warn);
                            Warnings.Add(warn);
                        } 
                    }

                    if (herdChange> 1.0)
                    {
                        aELocationNeeded = Math.Max(0, (totalAE * herdChange) - purchaseAE);
                        AeToRestock += aELocationNeeded;
                        double notHandled = HandleRestocking(aELocationNeeded, newgroup.Key, newgroup.FirstOrDefault());
                        AeRestocked += (aELocationNeeded - notHandled);
                    }
                    else if(herdChange < 1.0)
                    {
                        aELocationNeeded = Math.Max(0, (totalAE * (1 - herdChange)) - markedForSaleAE);
                        AeToDestock += aELocationNeeded;
                        double notHandled = HandleDestocking(AeToDestock, newgroup.Key);
                        AeDestocked += (aELocationNeeded - notHandled);
                    }
                }

                if(this.Status != ActivityStatus.Warning & AeToDestock + AeToRestock > 0)
                {
                    if(Math.Max(0,AeToRestock - AeRestocked) + Math.Max(0, AeToDestock - AeDestocked) == 0)
                    {
                        this.Status = ActivityStatus.Success;
                    }
                    else
                    {
                        this.Status = ActivityStatus.Partial;
                    }
                }
            }
        }

        /// <summary>
        /// Method to perform destocking
        /// </summary>
        /// <param name="animalEquivalentsForSale"></param>
        /// <param name="paddockName"></param>
        /// <returns>The AE that were not handled</returns>
        private double HandleDestocking(double animalEquivalentsForSale, string paddockName)
        {
            if (animalEquivalentsForSale <= 0)
                return 0;

            // move to underutilised paddocks
            // TODO: This can be added later as an activity including spelling

            // remove all potential purchases from list as they can't be supported.
            // This does not change the shortfall AE as they were not counted in TotalAE pressure.
            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            ruminantHerd.PurchaseIndividuals.RemoveAll(a => a.Location == paddockName);

            var destockGroups = FindAllChildren<RuminantGroup>().Where(a => a.Reason == RuminantStockGroupStyle.Destock);
            if (destockGroups.Count() == 0)
            {
                string warn = $"No [f=FilterGroup]s with a [Destock] Reason were provided in [a={this.Name}]\r\nNo destocking will be performed.";
                this.Status = ActivityStatus.Warning;
                if (!Warnings.Exists(warn))
                {
                    Summary.WriteWarning(this, warn);
                    Warnings.Add(warn);
                }
            }

            // remove individuals to sale as specified by destock groups
            foreach (RuminantGroup item in destockGroups)
            {
                // works with current filtered herd to obey filtering.
                List<Ruminant> herd = this.CurrentHerd(false).Where(a => a.Location == paddockName && !a.ReadyForSale).ToList();
                herd = herd.Filter(item);
                int cnt = 0;
                while (cnt < herd.Count() && animalEquivalentsForSale > 0)
                {
                    if (herd[cnt].SaleFlag != HerdChangeReason.DestockSale)
                    {
                        animalEquivalentsForSale -= herd[cnt].AdultEquivalent;
                        herd[cnt].SaleFlag = HerdChangeReason.DestockSale;
                    }
                    cnt++;
                }
                if (animalEquivalentsForSale <= 0)
                {
                    return 0;
                }
            }
            return animalEquivalentsForSale;

            // handling of sucklings with sold female is in RuminantActivityBuySell
            // buy or sell is handled by the buy sell activity
        }

        private double HandleRestocking(double animalEquivalentsToBuy, string paddockName, Ruminant exampleRuminant)
        {
            if (animalEquivalentsToBuy <= 0) 
                return 0;

            GrazeFoodStoreType foodStore = Resources.GetResourceItem(this, typeof(GrazeFoodStore), paddockName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;

            // ensure min pasture for restocking
            if ((foodStore == null) || ((foodStore.TonnesPerHectare * 1000) > MinimumFeedBeforeRestock))
            {
                var specifyComponents = FindAllChildren<SpecifyRuminant>();
                if (specifyComponents.Count() == 0)
                {
                    string warn = $"No [f=SpecifyRuminant]s were provided in [a={this.Name}]\r\nNo restocking will be performed.";
                    this.Status = ActivityStatus.Warning;
                    if (!Warnings.Exists(warn))
                    {
                        Summary.WriteWarning(this, warn);
                        Warnings.Add(warn);
                    }
                }

                // buy animals specified in restock ruminant groups
                foreach (SpecifyRuminant item in specifyComponents)
                {
                    double sumAE = 0;
                    double limitAE = animalEquivalentsToBuy * item.Proportion;

                    while (sumAE < limitAE && animalEquivalentsToBuy > 0)
                    {
                        Ruminant newIndividual = item.Details.CreateIndividuals(1).FirstOrDefault();
                        newIndividual.Location = paddockName;
                        newIndividual.BreedParams = item.BreedParams;
                        newIndividual.HerdName = item.BreedParams.Name;
                        newIndividual.PurchaseAge = newIndividual.Age;
                        newIndividual.SaleFlag = HerdChangeReason.RestockPurchase;

                        if(newIndividual.Weight == 0)
                        {
                            throw new ApsimXException(this, "Specified individual added during restock cannot have no weight");
                        }

                        Resources.RuminantHerd().PurchaseIndividuals.Add(newIndividual);
                        double indAE = newIndividual.AdultEquivalent;
                        animalEquivalentsToBuy -= indAE;
                        sumAE += indAE;
                    }
                }
                return Math.Max(0,animalEquivalentsToBuy);
            }
            return animalEquivalentsToBuy;
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
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
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
                bool extracomps = false;
                htmlWriter.Write("\r\n<div class=\"activityentry\">Monthly SOI data are provided by ");
                if (MonthlySOIFile == null || MonthlySOIFile == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">File not set</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"filelink\">" + MonthlySOIFile + "</span>");
                }
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n<div class=\"activityentry\">The mean of the previous ");
                if(AssessMonths == 0)
                {
                    htmlWriter.Write("<span class=\"errorlink\">Not set</span>");
                }
                else
                {
                    htmlWriter.Write($"<span class=\"setvalue\">{AssessMonths}</span>");
                }
                htmlWriter.Write($" months will determine the current ENSO phase where:");
                htmlWriter.Write("</div>");

                // when in El Nino
                htmlWriter.Write("\r\n<div class=\"activitybannerlight\">El Ni&ntilde;o phase</div>");
                htmlWriter.Write("\r\n<div class=\"activitycontentlight\">");
                htmlWriter.Write($"\r\n<div class=\"activityentry\">Mean SOI less than <span class=\"setvalue\">{SOIForElNino}</span></div>");

                // relationship to use
                var relationship = this.FindAllChildren<Relationship>().Where(a => a.Name.ToLower().Contains("nino")).FirstOrDefault();
                if(relationship is null)
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\"><span class=\"errorlink\">No <span class=\"otherlink\">Relationship</span> provided!</span> No herd change will be calculated for this phase</div>");
                }
                else
                {
                    extracomps = true;
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Herd change will be calculated from <span class=\"otherlink\">Relationship.{relationship.Name}</span> provided below</div>");
                }

                htmlWriter.Write("</div>");


                // when in La Nina
                htmlWriter.Write("\r\n<div class=\"activitybannerlight\">La Ni&ntilde;a phase</div>");
                htmlWriter.Write("\r\n<div class=\"activitycontentlight\">");
                htmlWriter.Write($"\r\n<div class=\"activityentry\">Mean SOI greater than <span class=\"setvalue\">{SOIForLaNina}</span></div>");

                // relationship to use
                relationship = this.FindAllChildren<Relationship>().Where(a => a.Name.ToLower().Contains("nina")).FirstOrDefault();
                if (relationship is null)
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\"><span class=\"errorlink\">No <span class=\"otherlink\">Relationship</span> provided!</span> No herd change will be calculated for this phase</div>");
                }
                else
                {
                    extracomps = true;
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Herd change will be calculated from <span class=\"otherlink\">Relationship.{relationship.Name}</span> provided below</div>");
                }

                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activitybannerlight\">Herd change</div>");
                // Destock
                htmlWriter.Write("\r\n<div class=\"activitycontentlight\">");
                var rumGrps = FindAllChildren<RuminantGroup>().Where(a => a.Reason == RuminantStockGroupStyle.Destock);
                if (rumGrps.Count() == 0)
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\"><span class=\"errorlink\">No <span class=\"filterlink\">RuminantGroups</span> with Reason <span class=\"setvalue\">Destock</span> were provided</span>. No destocking will be performed</div>");
                }
                else
                {
                    extracomps = true;
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Destocking will be performed in the order of <span class=\"filterlink\">RuminantGroups</span> with Reason <span class=\"setvalue\">Destock</span> provided below</div>");
                }

                // restock
                // pasture
                var specs = FindAllChildren<SpecifyRuminant>();
                if(specs.Count() == 0)
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\"><span class=\"errorlink\">No <span class=\"resourcelink\">SpecifyRuminant</span> were provided</span>. No restocking will be performed</div>");
                }
                else
                {
                    extracomps = true;
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Restocking will be performed in the order of <span class=\"resourcelink\">SpecifyRuminant</span> provided below</div>");
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">Restocking will be only take place when pasture biomass is above {MinimumFeedBeforeRestock} kg per hectare</div>");
                }
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div style=\"margin-top:10px;\" class=\"activitygroupsborder\">");
                if (extracomps)
                {
                    htmlWriter.Write("<div class=\"labournote\">Additional components used by this activity</div>");
                }
                else
                {
                    htmlWriter.Write("<div class=\"labournote\">No additional components have been supplied</div>");
                }

                return htmlWriter.ToString();
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            return "</div>";
        }
        #endregion
    }

    /// <summary>
    /// ENSO state
    /// </summary>
    public enum ENSOState
    {
        /// <summary>
        /// Neutral conditions
        /// </summary>
        Neutral,
        /// <summary>
        /// El Nino conditions
        /// </summary>
        ElNino,
        /// <summary>
        /// La Nina conditions
        /// </summary>
        LaNina

    }

}
