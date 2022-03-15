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
using Models.CLEM.Interfaces;

namespace Models.CLEM.Activities
{
    /// <summary>Manage trade herd activity</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manage a herd of individuals as trade herd")]
    [Version(1, 0, 1, "")]
    [Version(1, 0, 2, "Includes improvements such as a relationship to define numbers purchased based on pasture biomass and allows placement of purchased individuals in a specified paddock")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantBuy.htm")]
    public class RuminantActivityRequestPurchase : CLEMRuminantActivityBase, IValidatableObject, IHandlesActivityCompanionModels
    {
        private string grazeStore = "";
        private RuminantType herdToUse;
        private Relationship numberToStock;
        private GrazeFoodStoreType foodStore;

        /// <summary>
        /// GrazeFoodStore (paddock) to place purchases in for grazing
        /// </summary>
        [Category("General", "Pasture details")]
        [Description("GrazeFoodStore (paddock) to place purchases in")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Not specified - general yards", typeof(GrazeFoodStore) } })]
        [System.ComponentModel.DefaultValue("Not specified - general yards")]
        public string GrazeFoodStoreName { get; set; }

        // TODO: decide how many to stock.
        // stocking rate for paddock
        // fixed number

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityRequestPurchase()
        {
            this.SetDefaults();
            TransactionCategory = "Livestock.Buy";
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "RuminantGroup":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        units: new List<string>()
                        );
                case "RuminantActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                            "Number purchased",
                        },
                        units: new List<string>() {
                            "fixed",
                            "per head"
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(false, false);

            // get herd to add to 
            herdToUse = Resources.FindResourceType<RuminantHerd, RuminantType>(this, this.PredictedHerdName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            if(!herdToUse.PricingAvailable())
                Summary.WriteMessage(this, "No pricing is supplied for herd ["+PredictedHerdName+"] and so no pricing will be included with ["+this.Name+"]", MessageType.Warning);

            // check GrazeFoodStoreExists
            grazeStore = "";
            if (GrazeFoodStoreName != null && !GrazeFoodStoreName.StartsWith("Not specified"))
                grazeStore = GrazeFoodStoreName.Split('.').Last();

            // check for managed paddocks and warn if animals placed in yards.
            if (grazeStore == "")
            {
                var ah = this.FindInScope<ActivitiesHolder>();
                if (ah.FindAllDescendants<PastureActivityManage>().Count() != 0)
                    Summary.WriteMessage(this, String.Format("Trade animals purchased by [a={0}] are currently placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until moved and will require feeding while in yards.\r\nSolution: Set the [GrazeFoodStore to place purchase in] located in the properties [General].[PastureDetails]", this.Name), MessageType.Warning);
            }

            numberToStock = this.FindAllChildren<Relationship>().FirstOrDefault() as Relationship;
            if(numberToStock != null)
            {
                if (grazeStore != "")
                    foodStore = Resources.FindResourceType<GrazeFoodStore, GrazeFoodStoreType>(this, GrazeFoodStoreName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            }
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalManage")]
        protected override void OnGetResourcesPerformActivity(object sender, EventArgs e)
        {
            ManageActivityResourcesAndTasks();
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
                HerdResource.PurchaseIndividuals.RemoveAll(a => a.Breed == this.PredictedHerdBreed && a.SaleFlag == HerdChangeReason.TradePurchase);

                foreach (SpecifyRuminant purchaseSpecific in this.FindAllChildren<SpecifyRuminant>())
                {
                    RuminantTypeCohort purchasetype = purchaseSpecific.FindChild<RuminantTypeCohort>();
                    double number = purchasetype.Number;
                    if(numberToStock != null && foodStore != null)
                        //NOTE: ensure calculation method in relationship is fixed values
                        number = Convert.ToInt32(numberToStock.SolveY(foodStore.TonnesPerHectare), CultureInfo.InvariantCulture);

                    number *= purchaseSpecific.Proportion;

                    for (int i = 0; i < Math.Ceiling(number); i++)
                    {
                        double u1 = RandomNumberGenerator.Generator.NextDouble();
                        double u2 = RandomNumberGenerator.Generator.NextDouble();
                        double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                     Math.Sin(2.0 * Math.PI * u2);
                        double weight = purchasetype.Weight + purchasetype.WeightSD * randStdNormal;

                        var ruminant = Ruminant.Create(purchasetype.Sex, herdToUse, purchasetype.Age, weight);

                        ruminant.ID = 0;
                        ruminant.Breed = purchaseSpecific.BreedParams.Name;
                        ruminant.HerdName = purchaseSpecific.BreedParams.Breed;
                        ruminant.PurchaseAge = purchasetype.Age;
                        ruminant.SaleFlag = HerdChangeReason.TradePurchase;
                        ruminant.Location = grazeStore;
                        ruminant.PreviousWeight = ruminant.Weight;

                        // add trade tag for this trade activity
                        ruminant.Attributes.Add($"Trade:{Name}");

                        if (ruminant is RuminantFemale female)
                        {
                            female.WeightAtConception = ruminant.Weight;
                            female.NumberOfBirths = 0;
                        }

                        HerdResource.PurchaseIndividuals.Add(ruminant);
                        this.Status = ActivityStatus.Success;
                    }
                }
            }
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
            var specifyRuminants = this.FindAllChildren<SpecifyRuminant>();
            if (specifyRuminants.Count() == 0)
            {
                string[] memberNames = new string[] { "PurchaseDetails" };
                results.Add(new ValidationResult("At least one trade purchase description is required. Provide a [r=SpecifyRuminant] component below this activity specifying the breed and details of individuals to be purchased.", memberNames));
            }
            else
            {
                foreach (SpecifyRuminant specRumItem in specifyRuminants)
                {
                    // get Cohort
                    var items = specRumItem.FindAllChildren<RuminantTypeCohort>();
                    if (items.Count() > 1)
                    {
                        string[] memberNames = new string[] { "SpecifyRuminant cohort" };
                        results.Add(new ValidationResult("Each [r=SpecifyRuminant] can only contain one [r=RuminantTypeCohort]. Additional components will be ignored!", memberNames));
                    }
                    if (items.First().Suckling)
                    {
                        string[] memberNames = new string[] { "PurchaseDetails[Suckling]" };
                        results.Add(new ValidationResult("Suckling individuals are not permitted as ruminant purchases.", memberNames));
                    }
                }
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">The following individuals were be requested for purchase ");
                htmlWriter.Write("</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("Purchased individuals will be placed in ");
                if (GrazeFoodStoreName == null || GrazeFoodStoreName == "")
                    htmlWriter.Write("<span class=\"resourcelink\">General yards</span>");
                else
                    htmlWriter.Write("<span class=\"resourcelink\">" + GrazeFoodStoreName + "</span>");

                htmlWriter.Write("</div>");

                Relationship numberRelationship = this.FindAllChildren<Relationship>().FirstOrDefault() as Relationship;
                if (numberRelationship != null)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    if (GrazeFoodStoreName != null && !GrazeFoodStoreName.StartsWith("Not specified"))
                        htmlWriter.Write("The relationship <span class=\"activitylink\">" + numberRelationship.Name + "</span> will be used to calculate numbers purchased based on pasture biomass (t\\ha)");
                    else
                        htmlWriter.Write("The number of individuals in the Ruminant Cohort supplied will be used as no paddock has been supplied for the relationship <span class=\"resourcelink\">" + numberRelationship.Name + "</span> will be used to calulate numbers purchased based on pasture biomass (t//ha)");

                    htmlWriter.Write("</div>");
                }
                return htmlWriter.ToString(); 
            }
        } 
        #endregion
    }
}
