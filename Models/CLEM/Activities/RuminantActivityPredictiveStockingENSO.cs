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
    public class RuminantActivityPredictiveStockingENSO: CLEMRuminantActivityBase
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Minimum estimated feed (kg/ha) before restocking
        /// </summary>
        [Description("Minimum estimated feed (kg/ha) before restocking")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumFeedBeforeRestock { get; set; }

        /// <summary>
        /// Name of GrazeFoodStore (paddock) to place purchases in for grazing (leave blank for general yards)
        /// </summary>
        [Description("Name of GrazeFoodStore (paddock) to place purchases in (leave blank for general yards)")]
        public string GrazeFoodStoreName { get; set; }

        /// <summary>
        /// File containing SOI measure from BOM http://www.bom.gov.au/climate/influences/timeline/
        /// Year Jan Feb Mar.....
        /// 1876 11  0.2 -3  +ve LaNina -ve El Nino
        /// </summary>
        [Description("SOI monthly data file")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "SOI monthly data filename required")]
        [Models.Core.Display(Type = DisplayType.FileName)]
        public string MonthlySIOFile { get; set; }

        /// <summary>
        /// Mean SOI value before considered La Niña
        /// </summary>
        [Description("SOI cutoff before considered La Niña")]
        [Required, GreaterThanEqualValue(0)]
        public double MeanSOIForLaNina { get; set; }

        /// <summary>
        /// Mean  SOI value before considered El Niño
        /// </summary>
        [Description("SOI cutoff (-ve) before considered El Niño")]
        [Required, GreaterThanEqualValue(0)]
        public double MeanSOIForElNino { get; set; }

        /// <summary>
        /// The relationship to convert pasture biomass to stock rate change for El Niño
        /// </summary>
        [Description("El Niño pasture biomass to herd change relationship")]
        [Required]
        public Relationship PastureToStockingChangeElNino { get; set; }

        /// <summary>
        /// The relationship to convert pasture biomass to stock rate change for La Niña
        /// </summary>
        [Description("La Niña pasture biomass to herd change relationship")]
        [Required]
        public Relationship PastureToStockingChangeLaNina { get; set; }

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
        /// AE to restock
        /// </summary>
        [field: NonSerialized]
        public double AeToRestock { get; private set; }

        /// <summary>
        /// AE restocked
        /// </summary>
        [field: NonSerialized]
        public double AeRestocked { get; private set; }

        private GrazeFoodStoreType foodStore;
        private string fullFilename;
        private Dictionary<DateTime, double> ForecastSequence;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityPredictiveStockingENSO()
        {
            this.SetDefaults();
        }

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
                fullFilename = PathUtilities.GetAbsolutePath(this.MonthlySIOFile, simulation.FileName);
            }
            else
            {
                fullFilename = this.MonthlySIOFile;
            }

            //check file exists
            if (!File.Exists(fullFilename))
            {
                Summary.WriteWarning(this, String.Format("@error:Could not find ENSO SIO datafile [x={0}[ for [a={1}]", MonthlySIOFile, this.Name));
            }

            // load ENSO file into memory
            using (StreamReader ensoStream = new StreamReader(MonthlySIOFile))
            {
                string line = "";
                while((line = ensoStream.ReadLine())!=null)
                {
                    if(!line.StartsWith("Year"))
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

            this.InitialiseHerd(false, true);

            // check GrazeFoodStoreExists
            if (GrazeFoodStoreName == null)
            {
                GrazeFoodStoreName = "";
            }

            if (GrazeFoodStoreName != "")
            {
                foodStore = Resources.GetResourceItem(this, typeof(GrazeFoodStore), GrazeFoodStoreName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
            }
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
            if(ensoValue <= MeanSOIForElNino)
            {
                return ENSOState.ElNino;
            }
            else if (ensoValue >= MeanSOIForLaNina)
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

                // calculate dry season pasture available for each managed paddock holding stock
                RuminantHerd ruminantHerd = Resources.RuminantHerd();
                foreach (var newgroup in ruminantHerd.Herd.Where(a => a.Location != "").GroupBy(a => a.Location))
                {
                    // total adult equivalents of all breeds on pasture for utilisation
                    double totalAE = newgroup.Sum(a => a.AdultEquivalent);
                    // determine AE marked for sale and purchase of managed herd
                    double markedForSaleAE = newgroup.Where(a => a.ReadyForSale).Sum(a => a.AdultEquivalent);
                    double purchaseAE = ruminantHerd.PurchaseIndividuals.Where(a => a.Location == newgroup.Key).Sum(a => a.AdultEquivalent);

                    double herdChange = 1.0;
                    switch (forecastEnsoState)
                    {
                        case ENSOState.Neutral:
                            break;
                        case ENSOState.ElNino:
                            GrazeFoodStoreType pasture = Resources.GetResourceItem(this, typeof(GrazeFoodStoreType), newgroup.Key, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as GrazeFoodStoreType;
                            double kgha = pasture.TonnesPerHectare * 1000;
                            herdChange = this.PastureToStockingChangeElNino.SolveY(kgha);
                            break;
                        case ENSOState.LaNina:
                            pasture = Resources.GetResourceItem(this, typeof(GrazeFoodStoreType), newgroup.Key, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as GrazeFoodStoreType;
                            kgha = pasture.TonnesPerHectare * 1000;
                            herdChange = this.PastureToStockingChangeLaNina.SolveY(kgha);
                            break;
                        default:
                            break;
                    }
                    if(herdChange> 1.0)
                    {
                        AeToRestock = Math.Max(0, (totalAE*herdChange) - purchaseAE);
                        HandleRestocking(AeToRestock, newgroup.Key, newgroup.FirstOrDefault());
                    }
                    else if(herdChange < 1.0)
                    {
                        AeToDestock = Math.Max(0, (totalAE*(1-herdChange)) - markedForSaleAE);
                        HandleDestocking(AeToDestock, newgroup.Key);
                    }
                }
            }
        }

        private void HandleDestocking(double animalEquivalentsForSale, string paddockName)
        {
            if (animalEquivalentsForSale <= 0)
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
            foreach (RuminantGroup item in FindAllChildren<RuminantGroup>().Where(a => a.Reason == RuminantStockGroupStyle.Destock))
            {
                // works with current filtered herd to obey filtering.
                List<Ruminant> herd = this.CurrentHerd(false).Where(a => a.Location == paddockName && !a.ReadyForSale).ToList();
                herd = herd.Filter(item);
                int cnt = 0;
                while (cnt < herd.Count() && animalEquivalentsForSale > 0)
                {
                    this.Status = ActivityStatus.Success;
                    if (herd[cnt].SaleFlag != HerdChangeReason.DestockSale)
                    {
                        animalEquivalentsForSale -= herd[cnt].AdultEquivalent;
                        herd[cnt].SaleFlag = HerdChangeReason.DestockSale;
                    }
                    cnt++;
                }
                if (animalEquivalentsForSale <= 0)
                {
                    AeDestocked = 0;
                    this.Status = ActivityStatus.Success;
                    return;
                }
            }
            AeDestocked = AeToDestock - animalEquivalentsForSale;
            this.Status = ActivityStatus.Partial;

            // handling of sucklings with sold female is in RuminantActivityBuySell
            // buy or sell is handled by the buy sell activity
        }

        private void HandleRestocking(double animalEquivalentsToBuy, string paddockName, Ruminant exampleRuminant)
        {
            if (animalEquivalentsToBuy <= 0)
            {
                AeRestocked = 0;
                this.Status = ActivityStatus.Ignored;
                return;
            }

            // buy animals specified in restock ruminant groups

            // ensure min pasture for restocking
            if ((foodStore == null) || ((foodStore.TonnesPerHectare * 1000) > MinimumFeedBeforeRestock))
            {
                // for each define individual 
                // only for set proportion of AE required.

                // only set weight if not provided





                    double weight = exampleRuminant.StandardReferenceWeight - ((1 - exampleRuminant.BreedParams.SRWBirth) * exampleRuminant.StandardReferenceWeight) * Math.Exp(-(exampleRuminant.BreedParams.AgeGrowthRateCoefficient * (exampleRuminant.Age * 30.4)) / (Math.Pow(exampleRuminant.StandardReferenceWeight, exampleRuminant.BreedParams.SRWGrowthScalar)));
                double numberToBuy = aEtoBuy * Math.Pow(weight, 0.75) / Math.Pow(exampleRuminant.BreedParams.BaseAnimalEquivalent, 0.75); // convert to AE

                for (int i = 0; i < Convert.ToInt32(numberToBuy, CultureInfo.InvariantCulture); i++)
                {
                    Resources.RuminantHerd().PurchaseIndividuals.Add(new RuminantMale(192, Sex.Male, weight, exampleRuminant.BreedParams)
                    {
                        // Age = 192, or 16 months
                        HerdName = exampleRuminant.HerdName,
                        Number = 1,
                        SaleFlag = HerdChangeReason.RestockPurchase,
                        Breed = exampleRuminant.Breed,
                        Sire = false,
                        IsDraught = false,
                        Location = paddockName,
                    }
                    );
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
