using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using System.Globalization;
using System.IO;

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
    [Version(1, 0, 2, "Includes improvements such as a relationship to define numbers purchased based on pasture biomass and allows placement of purchased individuals in a specified paddock")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantTrade.htm")]
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

        /// <summary>
        /// GrazeFoodStore (paddock) to place purchases in for grazing
        /// </summary>
        [Category("General", "Pasture details")]
        [Description("GrazeFoodStore (paddock) to place purchases in")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(GrazeFoodStore) }, CLEMExtraEntries = new string[] { "Not specified - general yards" })]
        public string GrazeFoodStoreName { get; set; }

        private string grazeStore = "";
        private RuminantType herdToUse;
        private Relationship numberToStock;
        private GrazeFoodStoreType foodStore;

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

        #region validation
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
                if (item.Suckling)
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
        #endregion

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

            // check GrazeFoodStoreExists
            grazeStore = "";
            if (GrazeFoodStoreName != null && !GrazeFoodStoreName.StartsWith("Not specified"))
            {
                grazeStore = GrazeFoodStoreName.Split('.').Last();
            }

            // check for managed paddocks and warn if animals placed in yards.
            if (grazeStore == "")
            {
                var ah = this.FindInScope<ActivitiesHolder>();
                if (ah.FindAllDescendants<PastureActivityManage>().Count() != 0)
                {
                    Summary.WriteWarning(this, String.Format("Trade animals purchased by [a={0}] are currently placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until moved and will require feeding while in yards.\r\nSolution: Set the [GrazeFoodStore to place purchase in] located in the properties [General].[PastureDetails]", this.Name));
                }
            }

            numberToStock = this.FindAllChildren<Relationship>().FirstOrDefault() as Relationship;
            if(numberToStock != null)
            {
                if (grazeStore != "")
                {
                    foodStore = Resources.GetResourceItem(this, GrazeFoodStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as GrazeFoodStoreType;
                }
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
                this.Status = ActivityStatus.NotNeeded;
                // remove any old potential sales from list as these will be updated here
                Resources.RuminantHerd().PurchaseIndividuals.RemoveAll(a => a.Breed == this.PredictedHerdBreed && a.SaleFlag == HerdChangeReason.TradePurchase);

                foreach (RuminantTypeCohort purchasetype in this.Children.Where(a => a.GetType() == typeof(RuminantTypeCohort)).Cast<RuminantTypeCohort>())
                {
                    double number = purchasetype.Number;
                    if(numberToStock != null && foodStore != null)
                    {
                        //NOTE: ensure calculation method in relationship is fixed values
                        number = Convert.ToInt32(numberToStock.SolveY(foodStore.TonnesPerHectare), CultureInfo.InvariantCulture);
                    }

                    for (int i = 0; i < number; i++)
                    {
                        object ruminantBase = null;

                        double u1 = RandomNumberGenerator.Generator.NextDouble();
                        double u2 = RandomNumberGenerator.Generator.NextDouble();
                        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                     Math.Sin(2.0 * Math.PI * u2);
                        double weight = purchasetype.Weight + purchasetype.WeightSD * randStdNormal;

                        if (purchasetype.Gender == Sex.Male)
                        {
                            ruminantBase = new RuminantMale(purchasetype.Age, purchasetype.Gender, weight, herdToUse);
                        }
                        else
                        {
                            ruminantBase = new RuminantFemale(purchasetype.Age, purchasetype.Gender, weight, herdToUse);
                        }

                        Ruminant ruminant = ruminantBase as Ruminant;
                        ruminant.ID = 0;
                        ruminant.Breed = this.PredictedHerdBreed;
                        ruminant.HerdName = this.PredictedHerdName;
                        ruminant.PurchaseAge = purchasetype.Age;
                        ruminant.SaleFlag = HerdChangeReason.TradePurchase;
                        ruminant.Location = grazeStore;
                        ruminant.PreviousWeight = ruminant.Weight;

                        switch (purchasetype.Gender)
                        {
                            case Sex.Male:
                                RuminantMale ruminantMale = ruminantBase as RuminantMale;
                                ruminantMale.Sire = false;
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
                        this.Status = ActivityStatus.Success;
                    }
                }
            }
            // sale details any timestep when conditions are met.
            foreach (Ruminant ind in this.CurrentHerd(true))
            {
                if (ind.Age - ind.PurchaseAge >= MinMonthsKept)
                {
                    ind.SaleFlag = HerdChangeReason.TradeSale;
                    this.Status = ActivityStatus.Success;
                }
                if (TradeWeight > 0 && ind.Weight >= TradeWeight)
                {
                    ind.SaleFlag = HerdChangeReason.TradeSale;
                    this.Status = ActivityStatus.Success;
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
                htmlWriter.Write("\r\n<div class=\"activityentry\">Trade individuals are kept for ");
                htmlWriter.Write("<span class=\"setvalue\">" + MinMonthsKept.ToString("#0.#") + "</span> months");
                if (TradeWeight > 0)
                {
                    htmlWriter.Write(" or until");
                    htmlWriter.Write("<span class=\"setvalue\">" + TradeWeight.ToString("##0.##") + "</span> kg");
                }
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("Purchased individuals will be placed in ");
                if (GrazeFoodStoreName == null || GrazeFoodStoreName == "")
                {
                    htmlWriter.Write("<span class=\"resourcelink\">General yards</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + GrazeFoodStoreName + "</span>");
                }
                htmlWriter.Write("</div>");

                Relationship numberRelationship = this.FindAllChildren<Relationship>().FirstOrDefault() as Relationship;
                if (numberRelationship != null)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    if (GrazeFoodStoreName != null && !GrazeFoodStoreName.StartsWith("Not specified"))
                    {
                        htmlWriter.Write("The relationship <span class=\"activitylink\">" + numberRelationship.Name + "</span> will be used to calculate numbers purchased based on pasture biomass (t\\ha)");
                    }
                    else
                    {
                        htmlWriter.Write("The number of individuals in the Ruminant Cohort supplied will be used as no paddock has been supplied for the relationship <span class=\"resourcelink\">" + numberRelationship.Name + "</span> will be used to calulate numbers purchased based on pasture biomass (t//ha)");
                    }
                    htmlWriter.Write("</div>");
                }
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
