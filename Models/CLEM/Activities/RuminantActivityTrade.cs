using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.Core.Attributes;

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
    [Version(1, 0, 1, "")]
    public class RuminantActivityTrade : CLEMRuminantActivityBase, IValidatableObject
    {
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

        private RuminantType herdToUse;

        //TODO: decide how many to stock.
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

            // get herd to add to 
            herdToUse = Resources.GetResourceItem(this, typeof(RuminantHerd), this.PredictedHerdName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as RuminantType;

            if(!herdToUse.PricingAvailable())
            {
                Summary.WriteWarning(this, "No pricing is supplied for herd ["+PredictedHerdName+"] and so no pricing will be included with ["+this.Name+"]");
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
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">Trade individuals are kept for at least ";
            html += "<span class=\"setvalue\">" + MinMonthsKept.ToString("#0.#") + "</span> months or until";
            html += "<span class=\"setvalue\">" + TradeWeight.ToString("##0.##") + "</span> kg ";
            html += "</div>";
            return html;
        }
    }
}
