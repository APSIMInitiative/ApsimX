using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Models.Core.Attributes;
using System.Globalization;
using System.IO;
using Models.CLEM.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.CLEM.Activities
{
    /// <summary>Add individual ruminants to the purchase request list</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manage a herd of individuals as trade herd")]
    [Version(1, 1, 0, "Replaces old Trade herd approach")]
    [Version(1, 0, 2, "Includes improvements such as a relationship to define numbers purchased based on pasture biomass and allows placement of purchased individuals in a specified paddock")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantPurchase.htm")]
    public class RuminantActivityPurchase : CLEMRuminantActivityBase, IValidatableObject, IHandlesActivityCompanionModels
    {
        [Link]
        private readonly Clock clock = null;
        private string grazeStore = "";
        private Relationship numberToStock;
        private GrazeFoodStoreType foodStore;
        private int numberToDo;
        private int numberToSkip;
        private RuminantType rumTypeToUse;

        /// <summary>
        /// GrazeFoodStore (paddock) to place purchases in for grazing
        /// </summary>
        [Description("GrazeFoodStore (paddock) to place purchases in")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "Not specified - general yards", typeof(GrazeFoodStore) } })]
        [System.ComponentModel.DefaultValue("Not specified - general yards")]
        public string GrazeFoodStoreName { get; set; }

        /// <summary>
        /// Tag label
        /// </summary>
        [Description("Label of tag to assign")]
        public string TagLabel { get; set; }

        /// <summary>
        /// Number to purchase
        /// </summary>
        [Description("Number to purchase")]
        [Required, GreaterThanEqualValue(0)]
        public int NumberToPurchase { get; set; }

        // TODO: decide how many to stock.
        // stocking rate for paddock
        // fixed number

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityPurchase()
        {
            SetDefaults();
            AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "RuminantGroup":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>()
                        );
                case "Relationship":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() { "Number to stock vs pasture" },
                        measures: new List<string>()
                        );
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>() {
                        },
                        measures: new List<string>() {
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
            InitialiseHerd(false, false);

            // get herd to add to 
            rumTypeToUse = Resources.FindResourceType<RuminantHerd, RuminantType>(this, PredictedHerdName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            // check GrazeFoodStoreExists
            grazeStore = "";
            if (GrazeFoodStoreName != null && !GrazeFoodStoreName.StartsWith("Not specified"))
                grazeStore = GrazeFoodStoreName.Split('.').Last();

            // check for managed paddocks and warn if animals placed in yards.
            if (grazeStore == "")
            {
                var ah = FindInScope<ActivitiesHolder>();
                if (ah.FindAllDescendants<PastureActivityManage>().Any())
                {
                    Summary.WriteMessage(this, String.Format("Trade animals purchased by [a={0}] are currently placed in [Not specified - general yards] while a managed pasture is available. These animals will not graze until moved and will require feeding while in yards.\r\nSolution: Set the [GrazeFoodStore to place purchase in] located in the properties [General].[PastureDetails]", this.Name), MessageType.Warning);
                }
            }

            numberToStock = FindAllChildren<Relationship>().Where(a => a.Identifier == "Number to stock vs pasture").FirstOrDefault();
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

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            numberToSkip = 0;
            numberToDo = NumberToPurchase;
            if (numberToStock != null && foodStore != null)
                //NOTE: ensure calculation method in relationship is fixed values
                numberToDo = Convert.ToInt32(numberToStock.SolveY(foodStore.TonnesPerHectare), CultureInfo.InvariantCulture);

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per head":
                        valuesForCompanionModels[valueToSupply.Key] = numberToDo;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var purchaseShort = shortfalls.Where(a => a.CompanionModelDetails.identifier == "Number to purchase").FirstOrDefault();
                if (purchaseShort != null)
                    numberToSkip = Convert.ToInt32(numberToDo * (1 - purchaseShort.Available / purchaseShort.Required));

                if (numberToSkip == numberToDo)
                {
                    Status = ActivityStatus.Warning;
                    AddStatusMessage("Resource shortfall prevented any action");
                }
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (numberToDo - numberToSkip > 0)
            {
                int purchased = 0;

                foreach (SpecifyRuminant purchaseSpecific in FindAllChildren<SpecifyRuminant>())
                {
                    int number = Convert.ToInt32(Math.Ceiling(numberToDo - numberToSkip * purchaseSpecific.Proportion));
                    if (number > 0)
                    {
                        RuminantTypeCohort purchasetype = purchaseSpecific.FindChild<RuminantTypeCohort>();
                        var purchaseIndividuals = purchasetype.CreateIndividuals(number, purchasetype.FindAllChildren<ISetAttribute>().ToList(), clock.Today, rumTypeToUse, false);

                        foreach (var ind in purchaseIndividuals)
                        {
                            ind.DateOfPurchase = clock.Today; // PurchaseAge = purchasetype.Age;
                            ind.SaleFlag = HerdChangeReason.TradePurchase;
                            ind.Location = grazeStore;
                            if ((TagLabel ?? "") != "")
                                ind.Attributes.Add(TagLabel);
                        }

                        HerdResource.PurchaseIndividuals.AddRange(purchaseIndividuals);
                        purchased += number;
                    }
                }
                SetStatusSuccessOrPartial(purchased != numberToDo);
            }
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // check that a RuminantTypeCohort is supplied to identify trade individuals.
            var specifyRuminants = FindAllChildren<SpecifyRuminant>();
            if (specifyRuminants.Count() == 0)
            {
                string[] memberNames = new string[] { "PurchaseDetails" };
                yield return new ValidationResult($"You must specify details for the individuals to be purchased.{Environment.NewLine}Provide a [r=SpecifyRuminant] component below this activity specifying the breed and details of individuals to be purchased.", memberNames);
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
                        yield return new ValidationResult("Each [r=SpecifyRuminant] can only contain one [r=RuminantTypeCohort]. Additional components will be ignored!", memberNames);
                    }
                    if (items.First().Suckling)
                    {
                        string[] memberNames = new string[] { "PurchaseDetails[Suckling]" };
                        yield return new ValidationResult("Suckling individuals are not permitted as ruminant purchases.", memberNames);
                    }
                }
            }
            if (FindAllChildren<Relationship>().Where(a => a.Identifier == "Number to stock vs pasture").Any())
            {
                double cumulativeProp = specifyRuminants.Select(a => a.Proportion).Sum();
                if(MathUtilities.FloatsAreEqual(cumulativeProp, 1.0) == false)
                {
                    string[] memberNames = new string[] { "SpecifyRuminant proportions" };
                    yield return new ValidationResult("The proportions specified for all [r=SpecifyRuminant] must add up to 1", memberNames);
                }
            }

            if (GrazeFoodStoreName.Contains("."))
            {
                ResourcesHolder resHolder = FindInScope<ResourcesHolder>();
                if (resHolder is null || resHolder.FindResourceType<GrazeFoodStore, GrazeFoodStoreType>(this, GrazeFoodStoreName) is null)
                {
                    string[] memberNames = new string[] { "Location is not valid" };
                    yield return new ValidationResult($"The location where ruminants are to be placed [r={GrazeFoodStoreName}] is not found.{Environment.NewLine}Ensure [r=GrazeFoodStore] is present and the [GrazeFoodStoreType] is present", memberNames);
                }
            }
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">");
            htmlWriter.Write($"Purchased individuals will be placed in {DisplaySummaryResourceTypeSnippet(GrazeFoodStoreName, nullGeneralYards: true)}</div>");

            Relationship numberRelationship = FindAllChildren<Relationship>().Where(a => a.Identifier == "Number to stock vs pasture").FirstOrDefault();
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
        #endregion
    }
}
