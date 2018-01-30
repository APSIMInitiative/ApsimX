using APSIM.Shared.Utilities;
using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;

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
    public class RuminantActivityPredictiveStockingENSO: CLEMActivityBase
    {
        [Link]
        Clock Clock = null;
        [Link]
        ISummary Summary = null;

        /// <summary>
        /// Herd to manage for dry season pasture availability
        /// </summary>
        [Description("Name of herd to manage")]
        [Required]
        public string HerdName { get; set; }

        /// <summary>
        /// Month for assessing dry season feed requirements
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(5)]
        [Description("Month for assessing dry season feed requirements (1-12)")]
        [Required, Month]
        public int AssessmentMonth { get; set; }

        /// <summary>
        /// Minimum estimated feed (kg/ha) before restocking
        /// </summary>
        [Description("Minimum estimated feed (kg/ha) before restocking")]
        [Required, GreaterThanEqualValue(0)]
        public double MinimumFeedBeforeRestock { get; set; }

        // restock proportion. I don't understand this.
        // Maximum % restock breeders/age group

        /// <summary>
        /// Allow dry cows to be sold if feed shortage
        /// </summary>
        [Description("Allow dry cows to be sold if feed shortage")]
        [Required]
        public bool SellDryCows { get; set; }

        /// <summary>
        /// Allow wet cows to be sold if feed shortage
        /// </summary>
        [Description("Allow wet cows to be sold if feed shortage")]
        [Required]
        public bool SellWetCows { get; set; }

        /// <summary>
        /// Allow steers to be sold if feed shortage
        /// </summary>
        [Description("Allow steers to be sold if feed shortage")]
        [Required]
        public bool SellSteers { get; set; }

        /// <summary>
        /// File containing SOI measure from BOM http://www.bom.gov.au/climate/influences/timeline/
        /// Year Jan Feb Mar.....
        /// 1876 11  0.2 -3  +ve LaNina -ve El Nino
        /// </summary>
        [Description("SOI monthly data file")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "SOI monthly data filename required")]
        public string MonthlySIOFile { get; set; }

        private string fullFilename;

        /// <summary>
        /// Name of GrazeFoodStore (paddock) to place purchases in for grazing (leave blank for general yards)
        /// </summary>
        [Description("Name of GrazeFoodStore (paddock) to place purchases in (leave blank for general yards)")]
        public string GrazeFoodStoreName { get; set; }

        private Dictionary<DateTime, double> ForecastSequence;

        /// <summary>
        /// Minimum mean SOI for La Nina
        /// </summary>
        [Description("Minimum mean SOI for La Nina")]
        [Required, GreaterThanEqualValue(0)]
        public double MeanSOIForLaNina { get; set; }

        /// <summary>
        /// Minimum mean SOI (absolute) for El Nino
        /// </summary>
        [Description("Minimum mean SOI (absolute) for El Nino")]
        [Required, GreaterThanEqualValue(0)]
        public double MeanSOIForElNino { get; set; }

        /// <summary>
        /// The relationship to convert pasture biomass to stock rate change for El Nino
        /// </summary>
        [Description("The relationship to convert last 6 months SOI to stock rate change proportion")]
        [Required]
        public Relationship PastureToStockingChangeElNino { get; set; }

        /// <summary>
        /// The relationship to convert pasture biomass to stock rate change for La Nina
        /// </summary>
        [Description("The relationship to convert last 6 months SOI to stock rate change proportion")]
        [Required]
        public Relationship PastureToStockingChangeLaNina { get; set; }

        /// <summary>
        /// Store graze 
        /// </summary>
        private GrazeFoodStoreType foodStore;

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
            // load ENSO file into memory

            Simulation simulation = Apsim.Parent(this, typeof(Simulation)) as Simulation;
            if (simulation != null)
                fullFilename = PathUtilities.GetAbsolutePath(this.MonthlySIOFile, simulation.FileName);
            else
                fullFilename = this.MonthlySIOFile;

            //check file exists
            if (!File.Exists(fullFilename))
            {
                Summary.WriteWarning(this, String.Format("Could not find ENSO SIO datafile {0} for {1}", MonthlySIOFile, this.Name));
            }

            // load data
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
                                new DateTime(Convert.ToInt16(items[0]), Convert.ToInt16(i), 1),
                                Convert.ToDouble(items[i])
                                );
                        }
                    }
                }
            }

            // check GrazeFoodStoreExists
            if (GrazeFoodStoreName == null) GrazeFoodStoreName = "";
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
            int monthsAvailable = ForecastSequence.Where(a => a.Key >= date & a.Key <= date.AddMonths(-6)).Count();
            // get sum of previous 6 months
            double ensoValue = ForecastSequence.Where(a => a.Key >= date & a.Key <= date.AddMonths(-6)).Sum(a => a.Value);
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
            // this event happens after management has marked individuals for purchase or sale.
            if (Clock.Today.Month == AssessmentMonth)
            {
                // Get ENSO forcase for current time
                ENSOState ForecastEnsoState = GetENSOMeasure();

                // calculate dry season pasture available for each managed paddock holding stock
                RuminantHerd ruminantHerd = Resources.RuminantHerd();
                foreach (var newgroup in ruminantHerd.Herd.Where(a => a.Location != "").GroupBy(a => a.Location))
                {
                    // total adult equivalents of all breeds on pasture for utilisation
                    double AETotal = newgroup.Sum(a => a.AdultEquivalent);
                    // determine AE marked for sale and purchase of managed herd
                    double AEmarkedForSale = newgroup.Where(a => a.ReadyForSale & a.HerdName == HerdName).Sum(a => a.AdultEquivalent);
                    double AEPurchase = ruminantHerd.PurchaseIndividuals.Where(a => a.Location == newgroup.Key & a.HerdName == HerdName).Sum(a => a.AdultEquivalent);

                    double herdChange = 1.0;
                    switch (ForecastEnsoState)
                    {
                        case ENSOState.Neutral:
                            break;
                        case ENSOState.ElNino:
                            GrazeFoodStoreType pasture = Resources.GetResourceItem(this, typeof(GrazeFoodStoreType), newgroup.Key, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as GrazeFoodStoreType;
                            double kgha = pasture.TonnesPerHectare * 1000;
                            herdChange = this.PastureToStockingChangeElNino.SolveY(kgha, false);
                            break;
                        case ENSOState.LaNina:
                            pasture = Resources.GetResourceItem(this, typeof(GrazeFoodStoreType), newgroup.Key, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as GrazeFoodStoreType;
                            kgha = pasture.TonnesPerHectare * 1000;
                            herdChange = this.PastureToStockingChangeLaNina.SolveY(kgha, false);
                            break;
                        default:
                            break;
                    }
                    if(herdChange> 1.0)
                    {
                        double AEtoBuy = Math.Max(0, (AETotal*herdChange) - AEPurchase);
                        HandleRestocking(AEtoBuy, newgroup.Key, newgroup.FirstOrDefault());
                    }
                    else if(herdChange < 1.0)
                    {
                        double AEtoSell = Math.Max(0, (AETotal*(1-herdChange)) - AEmarkedForSale);
                        HandleDestocking(AEtoSell, newgroup.Key);
                    }

                    //switch (ForecastEnsoState)
                    //{
                    //    case ENSOState.Neutral:
                    //        break;
                    //    case ENSOState.LaNina:
                    //        double AEtoBuy = 0;
                    //        GrazeFoodStoreType pasture = Resources.GetResourceItem(typeof(GrazeFoodStoreType), newgroup.Key, out available) as GrazeFoodStoreType;
                    //        double kgha = pasture.TonnesPerHectare * 1000;
                    //        if (kgha > 3000)
                    //        {
                    //            AEtoBuy = AETotal * 0.2; //Buy 20% of Total Animal Equivalents
                    //        }
                    //        else if (kgha < 1000)
                    //        {
                    //            AEtoBuy = 0; //Don't buy any nore
                    //        }
                    //        else
                    //        {
                    //            AEtoBuy = AETotal * 0.1; //Buy 10 % of Total Animal Equivalents
                    //        }
                    //        // adjust for the AE already marked for purchase if any
                    //        AEtoBuy = Math.Max(0, AEtoBuy - AEPurchase);
                    //        HandleRestocking(AEtoBuy, newgroup.Key, newgroup.FirstOrDefault());
                    //        break;
                    //    case ENSOState.ElNino:
                    //        pasture = Resources.GetResourceItem(typeof(GrazeFoodStoreType), newgroup.Key, out available) as GrazeFoodStoreType;
                    //        kgha = pasture.TonnesPerHectare * 1000;
                    //        if (kgha > 3000)
                    //        {
                    //            ShortfallAE = AETotal * 0.1; //Sell 10% of Total Animal Equivalents
                    //        }
                    //        else if (kgha < 1000)
                    //        {
                    //            ShortfallAE = AETotal * 0.3; //Sell 30% of Total Animal Equivalents
                    //        }
                    //        else
                    //        {
                    //            ShortfallAE = AETotal * 0.2; //Sell 20 % of Total Animal Equivalents
                    //        }
                    //        // Adjust for AE already flagged for sale if any
                    //        ShortfallAE = Math.Max(0, ShortfallAE - AEmarkedForSale);
                    //        HandleDestocking(ShortfallAE, newgroup.Key);
                    //        break;
                    //    default:
                    //        break;
                    //}
                }
            }
        }

        private void HandleDestocking(double AEforSale, string PaddockName)
        {
            if (AEforSale <= 0) return;

            // move to underutilised paddocks
            // TODO: This can be added later as an activity including spelling

            // remove potential purchases from list
            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            List<Ruminant> purchases = ruminantHerd.PurchaseIndividuals.Where(a => a.Location == PaddockName & a.HerdName == HerdName).ToList();
            while(purchases.Count()>0 & AEforSale>0)
            {
                AEforSale -= purchases[0].AdultEquivalent;
                purchases.RemoveAt(0);
                if (AEforSale < purchases.Min(a => a.AdultEquivalent))
                {
                    AEforSale = 0;
                }
            }
            if (AEforSale <= 0) return;

            // adjust remaining herd
            // remove steers
            if (this.SellSteers)
            {
                List<RuminantMale> steers = ruminantHerd.Herd.Where(a => a.Location == PaddockName & a.HerdName == HerdName & a.Gender == Sex.Male).Cast<RuminantMale>().Where(a => a.BreedingSire == false).ToList();
                int cnt = 0;
                while (cnt < steers.Count() & AEforSale > 0)
                {
                    AEforSale -= steers[cnt].AdultEquivalent;
                    steers[cnt].SaleFlag = HerdChangeReason.DestockSale;
                    if (AEforSale < steers.Min(a => a.AdultEquivalent))
                    {
                        AEforSale = 0;
                    }
                    cnt++;
                }
            }
            if (AEforSale <= 0) return;

            // remove additional dry breeders
            if (this.SellDryCows)
            {
                // find dry cows not already marked for sale
                List<RuminantFemale> drybreeders = ruminantHerd.Herd.Where(a => a.Location == PaddockName & a.HerdName == HerdName & a.Gender == Sex.Female & a.SaleFlag == HerdChangeReason.None).Cast<RuminantFemale>().Where(a => a.DryBreeder == true).ToList();
                int cnt = 0;
                while (cnt < drybreeders.Count() & AEforSale > 0)
                {
                    AEforSale -= drybreeders[cnt].AdultEquivalent;
                    drybreeders[cnt].SaleFlag = HerdChangeReason.DestockSale;
                    if (AEforSale < drybreeders.Min(a => a.AdultEquivalent))
                    {
                        AEforSale = 0;
                    }
                    cnt++;
                }
            }
            if (AEforSale <= 0) return;

            // remove wet breeders with no calf
            // currently ignore pregant
            // is lactating with no calves are sold.

            // TODO manage calves from sold wet breeders. eg move to yards
            if (this.SellWetCows)
            {
                // remove wet cows
                // find wet cows not already marked for sale
                List<RuminantFemale> wetbreeders = ruminantHerd.Herd.Where(a => a.Location == PaddockName & a.HerdName == HerdName & a.Gender == Sex.Female & a.SaleFlag == HerdChangeReason.None).Cast<RuminantFemale>().Where(a => a.IsLactating == true & a.SucklingOffspring.Count() == 0).ToList();
                int cnt = 0;
                while (cnt < wetbreeders.Count() & AEforSale > 0)
                {
                    AEforSale -= wetbreeders[cnt].AdultEquivalent;
                    wetbreeders[cnt].SaleFlag = HerdChangeReason.DestockSale;
                    if (AEforSale < wetbreeders.Min(a => a.AdultEquivalent))
                    {
                        AEforSale = 0;
                    }
                    cnt++;
                }
            }

            // buy or sell is handled by the buy sell activity
        }

        private void HandleRestocking(double AEtoBuy, string PaddockName, Ruminant exampleRuminant)
        {
            if (AEtoBuy <= 0) return;

            // we won't remove individuals from the sale pool as we can't assume we can keep them in the herd
            // as management has already decided they need to be sold.

            // buy steers to fatten up and take advantage of the good season growth.

            // ensure min pasture for restocking
            if ((foodStore == null) || ((foodStore.TonnesPerHectare * 1000) > MinimumFeedBeforeRestock))
            {
                double weight = exampleRuminant.StandardReferenceWeight - ((1 - exampleRuminant.BreedParams.SRWBirth) * exampleRuminant.StandardReferenceWeight) * Math.Exp(-(exampleRuminant.BreedParams.AgeGrowthRateCoefficient * (exampleRuminant.Age * 30.4)) / (Math.Pow(exampleRuminant.StandardReferenceWeight, exampleRuminant.BreedParams.SRWGrowthScalar)));
                double numberToBuy = AEtoBuy * Math.Pow(weight, 0.75) / Math.Pow(exampleRuminant.BreedParams.BaseAnimalEquivalent, 0.75); // convert to AE

                for (int i = 0; i < Convert.ToInt32(numberToBuy); i++)
                {
                    Resources.RuminantHerd().PurchaseIndividuals.Add(new RuminantMale()
                    {
                        Age = 192, // 16 months
                        Gender = Sex.Male,
                        HerdName = exampleRuminant.HerdName,
                        HighWeight = weight,
                        Number = 1,
                        SaleFlag = HerdChangeReason.RestockPurchase,
                        Breed = exampleRuminant.Breed,
                        BreedParams = exampleRuminant.BreedParams,
                        BreedingSire = false,
                        Draught = false,
                        Location = PaddockName,
                        Weight = weight
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
