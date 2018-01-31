using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.CLEM.Groupings;

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
    public class RuminantActivityPredictiveStocking: CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        Clock Clock = null;

        ///// <summary>
        ///// Herd to manage for dry season pasture availability
        ///// </summary>
        //[Description("Name of herd to manage")]
        //[Required]
        //public string HerdName { get; set; }

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

        // minimum no that can be sold off... now controlled by sale and transport activity 

        ///// <summary>
        ///// Minimum breedeer age allowed to be sold
        ///// </summary>
        //[Description("Minimum breedeer age allowed to be sold")]
        //[Required, Range(0, double.MaxValue, ErrorMessage = "Value must be a greter than or equal to 0")]
        //public double MinimumBreederAgeLimit { get; set; }

        // restock proportion. I don't understand this.
        // Maximum % restock breeders/age group

        ///// <summary>
        ///// Allow dry cows to be sold if feed shortage
        ///// </summary>
        //[Description("Allow dry cows to be sold if feed shortage")]
        //[Required]
        //public bool SellDryCows { get; set; }

        ///// <summary>
        ///// Allow wet cows to be sold if feed shortage
        ///// </summary>
        //[Description("Allow wet cows to be sold if feed shortage")]
        //[Required]
        //public bool SellWetCows { get; set; }

        ///// <summary>
        ///// Allow steers to be sold if feed shortage
        ///// </summary>
        //[Description("Allow steers to be sold if feed shortage")]
        //[Required]
        //public bool SellSteers { get; set; }

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
                if (destockGroupFound) break;
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
                // calculate dry season pasture available for each managed paddock holding stock not flagged for sale
                RuminantHerd ruminantHerd = Resources.RuminantHerd();
                foreach (var paddockGroup in ruminantHerd.Herd.Where(a => a.Location != "").GroupBy(a => a.Location))
                {
                    // multiple breeds are currently not supported as we need to work out what to do with diferent AEs
                    if(paddockGroup.GroupBy(a => a.Breed).Count() > 1)
                    {
                        throw new ApsimXException(this, "Dry season destocking paddocks with multiple breeds is currently not supported\nActivity:"+this.Name+", Paddock: "+paddockGroup.Key);
                    }

                    // total adult equivalents not marked for sale of all breeds on pasture for utilisation
                    double AETotal = paddockGroup.Where(a => a.SaleFlag == HerdChangeReason.None).Sum(a => a.AdultEquivalent);

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
                    // Shortfall in Fodder in kg for paddock
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

            // remove all potential purchases from list as they can't be supported.
            // This does not change the shortfall AE as they were not counted in TotalAE pressure.
            RuminantHerd ruminantHerd = Resources.RuminantHerd();
            ruminantHerd.PurchaseIndividuals.RemoveAll(a => a.Location == PaddockName);

            // remove individuals to sale as specified by destock groups
            foreach (RuminantDestockGroup item in this.Children.Where(a => a.GetType() == typeof(RuminantDestockGroup)))
            {
                // works with current filtered herd to obey filtering.
                List<Ruminant> herd = this.CurrentHerd(false).Where(a => a.Location == PaddockName & !a.ReadyForSale).ToList();
                herd = herd.Filter(item);
                int cnt = 0;
                while (cnt < herd.Count() & AEforSale > 0)
                {
                    AEforSale -= herd[cnt].AdultEquivalent;
                    herd[cnt].SaleFlag = HerdChangeReason.DestockSale;
                    if (AEforSale < herd.Min(a => a.AdultEquivalent))
                    {
                        AEforSale = 0;
                    }
                    cnt++;
                }
                if (AEforSale <= 0) return;
            }

            // Possible destock groups
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
