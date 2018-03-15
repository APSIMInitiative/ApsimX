using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

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
    public class RuminantActivityPredictiveStockingOrig: CLEMRuminantActivityBase
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Herd to manage for dry season pasture availability
        /// </summary>
        [Description("Name of herd to manage")]
        [Required]
        public string HerdName { get; set; }

        /// <summary>
        /// Month for assessing dry season feed requirements
        /// </summary>
        [Description("Month for assessing dry season feed requirements (1-12)")]
        [Required, Range(1, 12, ErrorMessage = "Value must represent a month from 1 (Jan) to 12 (Dec)")]
        public int AssessmentMonth { get; set; }

        /// <summary>
        /// Number of months to assess
        /// </summary>
        [Description("Number of months to assess")]
        [Required, Range(0, int.MaxValue, ErrorMessage = "Value must be a greter than or equal to 0")]
        public int DrySeasonLength { get; set; }

        /// <summary>
        /// Minimum estimated feed (kg/ha) allowed at end of period
        /// </summary>
        [Description("Minimum estimated feed (kg/ha) allowed at end of period")]
        [Required, Range(0, double.MaxValue, ErrorMessage = "Value must be a greter than or equal to 0")]
        public double FeedLowLimit { get; set; }

        /// minimum no that can be sold off... now controlled by sale and transport activity 

        /// <summary>
        /// Minimum breedeer age allowed to be sold
        /// </summary>
        [Description("Minimum breedeer age allowed to be sold")]
        [Required, Range(0, double.MaxValue, ErrorMessage = "Value must be a greter than or equal to 0")]
        public double MinimumBreederAgeLimit { get; set; }

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

        /// <summary>An event handler to call for all resources other than food for feeding activity</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalStock")]
        private void OnCLEMAnimalStock(object sender, EventArgs e)
        {
            // this event happens after management has marked individuals for purchase or sale.
            if (Clock.Today.Month == AssessmentMonth)
            {
                // calculate dry season pasture available for each managed paddock holding stock not flagged for sale
                RuminantHerd ruminantHerd = Resources.RuminantHerd();
                foreach (var paddockGroup in ruminantHerd.Herd.Where(a => a.Location != "" & a.SaleFlag == HerdChangeReason.None).GroupBy(a => a.Location))
                {
                    // total adult equivalents of all breeds on pasture for utilisation
                    double AETotal = paddockGroup.Sum(a => a.AdultEquivalent);
                    // determine AE marked for sale and purchase of managed herd
                    double AEmarkedForSale = paddockGroup.Where(a => a.ReadyForSale & a.HerdName == HerdName).Sum(a => a.AdultEquivalent);
                    double AEPurchase = ruminantHerd.PurchaseIndividuals.Where(a => a.Location == paddockGroup.Key & a.HerdName == HerdName).Sum(a => a.AdultEquivalent);

                    double ShortfallAE = 0;
                    // Determine total feed requirements for dry season for all ruminants on the pasture
                    // We assume that all ruminant have the BaseAnimalEquivalent to the specified herd
                    ShortfallAE = 0;
                    GrazeFoodStoreType pasture = Resources.GetResourceItem(this, typeof(GrazeFoodStoreType), paddockGroup.Key, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
                    double pastureBiomass = pasture.Amount;

                    // Adjust fodder balance for detachment rate (6%/month)
                    double feedRequiredAE = paddockGroup.FirstOrDefault().BreedParams.BaseAnimalEquivalent * 0.02 * 30.4; //  2% of AE animal per day
                    for (int i = 0; i < this.DrySeasonLength; i++)
                    {
                        pastureBiomass *= (1.0 - pasture.DetachRate);
                        pastureBiomass -= feedRequiredAE * AETotal;
                    }

                    // Shortfall in Fodder in kg per hectare
                    double pastureShortFallKgHa = pastureBiomass / pasture.Area;
                    pastureShortFallKgHa = Math.Max(0, pastureShortFallKgHa - FeedLowLimit);
                    // Shortfalll in Fodder in kg for paddock
                    double pastureShortFallKg = pastureShortFallKgHa * pasture.Area;

                    if (pastureShortFallKg == 0) return;

                    // number of AE to sell to balance shortfall_kg
                    ShortfallAE = pastureShortFallKg / feedRequiredAE;

                    // get prediction
                    HandleDestocking(ShortfallAE, paddockGroup.Key);
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
            while (purchases.Count() > 0 & AEforSale > 0)
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
}
