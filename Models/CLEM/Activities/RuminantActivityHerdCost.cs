using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.CLEM;
using Models.CLEM.Groupings;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>Ruminant herd cost </summary>
    /// <summary>This activity will arrange payment of a herd expense such as vet fees</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity will arange payment of a ruminant herd expense such as dips and drenches based on the current herd filtering.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantHerdCost.htm")]
    public class RuminantActivityHerdCost : CLEMRuminantActivityBase, IValidatableObject
    {
        /// <summary>
        /// Amount payable
        /// </summary>
        [Description("Amount payable")]
        [Required, GreaterThanEqualValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// Payment style
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(AnimalPaymentStyleType.perHead)]
        [Description("Payment style")]
        [Required]
        public AnimalPaymentStyleType PaymentStyle { get; set; }

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(Finance) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Bank account required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Validate object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            switch (PaymentStyle)
            {
                case AnimalPaymentStyleType.Fixed:
                case AnimalPaymentStyleType.perHead:
                case AnimalPaymentStyleType.perAE:
                    break;
                default:
                    string[] memberNames = new string[] { "PaymentStyle" };
                    results.Add(new ValidationResult("Payment style " + PaymentStyle.ToString() + " is not supported", memberNames));
                    break;
            }
            return results;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityHerdCost()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            this.InitialiseHerd(true, true);
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> resourcesNeeded = null;

            double amountNeeded = 0;
            List<Ruminant> herd = this.CurrentHerd(false);
            if (herd.Count() != 0)
            {
                switch (PaymentStyle)
                {
                    case AnimalPaymentStyleType.Fixed:
                        amountNeeded = Amount;
                        break;
                    case AnimalPaymentStyleType.perHead:
                        amountNeeded = Amount * herd.Count();
                        break;
                    case AnimalPaymentStyleType.perAE:
                        amountNeeded = Amount * herd.Sum(a => a.AdultEquivalent);
                        break;
                    default:
                        break;
                }
            }

            if (amountNeeded > 0)
            {
                // determine breed
                // this is too much overhead for a simple reason field, especially given large herds.
                //List<string> res = herd.Select(a => a.Breed).Distinct().ToList();
                //string breedName = (res.Count() > 1) ? "Multiple breeds" : res.First();
                string breedName = "Herd cost";

                resourcesNeeded = new List<ResourceRequest>()
                {
                    new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = amountNeeded,
                        ResourceType = typeof(Finance),
                        ResourceTypeName = this.AccountName.Split('.').Last(),
                        ActivityModel = this,
                        Reason = breedName
                    }
                };
            }
            return resourcesNeeded;
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
            // get all potential dry breeders
            List<Ruminant> herd = this.CurrentHerd(false);
            int head = herd.Count();
            double animalEquivalents = herd.Sum(a => a.AdultEquivalent);
            double daysNeeded = 0;
            double numberUnits = 0;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = head / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perAE:
                    numberUnits = animalEquivalents / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return daysNeeded;
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
            html += "\n<div class=\"activityentry\">Pay ";
            html += "<span class=\"setvalue\">" + Amount.ToString("#,##0.00") + "</span> ";
            html += "<span class=\"setvalue\">" + PaymentStyle.ToString() + "</span> from ";
            if (AccountName == null || AccountName == "")
            {
                html += "<span class=\"errorlink\">[ACCOUNT NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + AccountName + "</span>";
            }
            html += "</div>";
            return html;
        }

    }
}
