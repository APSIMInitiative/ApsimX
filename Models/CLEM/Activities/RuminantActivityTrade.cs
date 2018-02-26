using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant herd management activity</summary>
    /// <summary>This activity will maintain a breeding herd at the desired levels of age/breeders etc</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages trade individuals. It requires a RuminantActivityBuySell to undertake the sales and removal of individuals.")]
    public class RuminantActivityTrade : CLEMRuminantActivityBase, IValidatableObject
    {
        [Link]
        ISummary Summary = null;

        ///// <summary>
        ///// Name of herd to trade
        ///// </summary>
        //[Description("Name of herd to trade")]
        //      [Required]
        //      public string HerdName { get; set; }

  //      /// <summary>
  //      /// Weight of inividuals to buy
  //      /// </summary>
  //      [Description("Weight of inividuals to buy")]
  //      [Required]
  //      public double BuyWeight { get; set; }

        ///// <summary>
        ///// Animal age at purchase (months)
        ///// </summary>
        //[Description("Animal age at purchase (months)")]
  //      [Required]
  //      public int BuyAge { get; set; }

        ///// <summary>
        ///// Trade price (purchase/sell price /kg LWT)
        ///// </summary>
        //[Description("Trade price (purchase/sell price /kg LWT)")]
  //      [Required]
  //      public double TradePrice { get; set; }

        /// <summary>
        /// Months kept before sale
        /// </summary>
        [Description("Months kept before sale")]
        [Required, GreaterThanEqualValue(1)]
        public int MinMonthsKept { get; set; }

        /// <summary>
        /// Weight to achieve before sale
        /// </summary>
        [Description("Weight to achieve before sale")]
        [Required, GreaterThanEqualValue(0)]
        public double TradeWeight { get; set; }

        ///// <summary>
        ///// Purchase month
        ///// </summary>
        //[System.ComponentModel.DefaultValueAttribute(11)]
        //[Description("Purchase month")]
  //      [Required, Range(1, 12, ErrorMessage = "Value must represent a month from 1 (Jan) to 12 (Dec)")]
  //      public int PurchaseMonth { get; set; }

        private RuminantType herdToUse;
        private List<LabourFilterGroupSpecified> labour { get; set; }


        //TODO: devide how many to stock.
        // stocking rate for paddock
        // fixed number

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityTrade()
        {
            this.SetDefaults();
        }

        /// <summary>
        /// Validate this model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            // check that a RuminantTypeCohort is supplied to identify trade individuals.
            if (this.Children.Where(a => a.GetType() == typeof(RuminantTypeCohort)).Count() == 0)
            {
                string[] memberNames = new string[] { "PurchaseDetails" };
                results.Add(new ValidationResult("At least one trade pruchase description is required. Provide a RuminantTypeCohort model below this activity specifying the number, size and age of individuals to be purchased.", memberNames));
            }
            foreach (RuminantTypeCohort item in this.Children.Where(a => a.GetType() == typeof(RuminantTypeCohort)).Cast<RuminantTypeCohort>())
            {
                if(item.Suckling)
                {
                    string[] memberNames = new string[] { "PurchaseDetails[Suckling]" };
                    results.Add(new ValidationResult("Suckling individuals are not permitted as trade purchases.", memberNames));
                }
                if (item.Sire)
                {
                    string[] memberNames = new string[] { "PurchaseDetails[Sire]" };
                    results.Add(new ValidationResult("Sires are not permitted as trade purchases.", memberNames));
                }
            }


            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(false, false);

            // check if labour and warn it is not used for this activity
            labour = Apsim.Children(this, typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (labour != null)
            {
                Summary.WriteWarning(this, "Warning: Labour was supplied for activity ["+this.Name+"] but is not used for Trade activities. Please add labour requirements to the Buy/Sell Activity associated with this trade herd.");
            }

            // get herd to add to 
            herdToUse = Resources.GetResourceItem(this, typeof(RuminantHerd), this.PredictedHerdName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as RuminantType;

            if(!herdToUse.PricingAvailable())
            {
                Summary.WriteWarning(this, "Warning: No pricing is supplied for herd ["+PredictedHerdName+"] and so no pricing will be included with ["+this.Name+"]");
            }
        }

        /// <summary>An event handler to call for all herd management activities</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalManage")]
        private void OnCLEMAnimalManage(object sender, EventArgs e)
        {
            // purchase details only on timer
            if(TimingOK)
            {
                // remove any old potential sales from list as these will be updated here
                Resources.RuminantHerd().PurchaseIndividuals.RemoveAll(a => a.Breed == this.PredictedHerdBreed & a.SaleFlag == HerdChangeReason.TradePurchase);

                foreach (RuminantTypeCohort purchasetype in this.Children.Where(a => a.GetType() == typeof(RuminantTypeCohort)).Cast<RuminantTypeCohort>())
                {
                    for (int i = 0; i < purchasetype.Number; i++)
                    {
                        object ruminantBase = null;
                        if (purchasetype.Gender == Sex.Male)
                        {
                            ruminantBase = new RuminantMale();
                        }
                        else
                        {
                            ruminantBase = new RuminantFemale();
                        }

                        Ruminant ruminant = ruminantBase as Ruminant;
                        ruminant.ID = 0;
                        ruminant.BreedParams = herdToUse;
                        ruminant.Breed = this.PredictedHerdBreed;
                        ruminant.HerdName = this.PredictedHerdName;
                        ruminant.Gender = purchasetype.Gender;
                        ruminant.Age = purchasetype.Age;
                        ruminant.PurchaseAge = purchasetype.Age;
                        ruminant.SaleFlag = HerdChangeReason.TradePurchase;
                        ruminant.Location = "";

                        double u1 = ZoneCLEM.RandomGenerator.NextDouble();
                        double u2 = ZoneCLEM.RandomGenerator.NextDouble();
                        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                     Math.Sin(2.0 * Math.PI * u2);
                        ruminant.Weight = purchasetype.Weight + purchasetype.WeightSD * randStdNormal;
                        ruminant.PreviousWeight = ruminant.Weight;

                        switch (purchasetype.Gender)
                        {
                            case Sex.Male:
                                RuminantMale ruminantMale = ruminantBase as RuminantMale;
                                ruminantMale.BreedingSire = false;
                                break;
                            case Sex.Female:
                                RuminantFemale ruminantFemale = ruminantBase as RuminantFemale;
                                ruminantFemale.DryBreeder = true;
                                ruminantFemale.WeightAtConception = ruminant.Weight;
                                ruminantFemale.NumberOfBirths = 0;
                                break;
                            default:
                                break;
                        }

                        Resources.RuminantHerd().PurchaseIndividuals.Add(ruminantBase as Ruminant);
                    }
                }
                //this.TriggerOnActivityPerformed();
            }
            // sale details any timestep when conditions are met.
            foreach (Ruminant ind in this.CurrentHerd(true))
            {
                if (ind.Age - ind.PurchaseAge >= MinMonthsKept & ind.Weight >= TradeWeight)
                {
                    ind.SaleFlag = HerdChangeReason.TradeSale;
                }
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            // check for labour
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
