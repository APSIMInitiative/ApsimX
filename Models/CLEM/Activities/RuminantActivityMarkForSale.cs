using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Mark specified individual ruminants for sale.</summary>
    /// <summary>This activity is in addition to those identified in RuminantActivityManage</summary>
    /// <version>1.0</version>
    /// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Mark the specified individuals for sale with specified sale reason")]
    [Version(1, 0, 2, "Allows specification of sale reason for reporting")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantMarkForSale.htm")]
    public class RuminantActivityMarkForSale: CLEMRuminantActivityBase, IValidatableObject
    {
        private LabourRequirement labourRequirement;

        /// <summary>
        /// Sale flag to use
        /// </summary>
        [Description("Sale reason to apply")]
        [System.ComponentModel.DefaultValueAttribute("MarkedSale")]
        [GreaterThanValue(0, ErrorMessage = "A sale reason must be provided")]
        [HerdSaleReason("sale", ErrorMessage = "The herd change reason provided must relate to a sale")]
        public HerdChangeReason SaleFlagToUse { get; set; }

        /// <summary>
        /// Overwrite any currently recorded sale flag
        /// </summary>
        [Description("Overwrite existing sale flag")]
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool OverwriteFlag { get; set; }

        private int numberToTag = 0;
        private bool labourShortfall = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantActivityMarkForSale()
        {
            TransactionCategory = "Livestock.Manage";
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            this.InitialiseHerd(true, true);

            // activity is performed in ManageAnimals
            this.AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalMark")]
        protected override void OnGetResourcesPerformActivity(object sender, EventArgs e)
        {
        }

        /// <inheritdoc/>
        protected override List<ResourceRequest> DetermineResourcesForActivity()
        {
            numberToTag = NumberToTag();
            return null;
        }

        private int NumberToTag()
        {
            IEnumerable<Ruminant> herd = CurrentHerd(false);

            var filterGroups = FindAllChildren<RuminantGroup>();
            int number = 0;
            if (filterGroups.Any())
            {
                number = 0;
                foreach (RuminantGroup item in filterGroups)
                    number += item.Filter(herd).Where(a => OverwriteFlag || a.SaleFlag == HerdChangeReason.None).Count();

                if(number > 0)
                    number = Math.Min(number, herd.Count());
            }
            else
                number = herd.Count();

            return number;
        }

        /// <inheritdoc/>
        protected override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            labourRequirement = requirement;
            double daysNeeded;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    double numberUnits = numberToTag / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                        numberUnits = Math.Ceiling(numberUnits);

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new LabourRequiredArgs(daysNeeded, "Mark", this.PredictedHerdName);
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForActivity()
        {
            labourShortfall = false;
            if (LabourLimitProportion < 1 & (labourRequirement != null && labourRequirement.LabourShortfallAffectsActivity))
            {
                switch (labourRequirement.UnitType)
                {
                    case LabourUnitType.Fixed:
                    case LabourUnitType.perHead:
                        numberToTag = Convert.ToInt32(numberToTag * LabourLimitProportion, CultureInfo.InvariantCulture);
                        labourShortfall = true;
                        break;
                    default:
                        throw new ApsimXException(this, "Labour requirement type " + labourRequirement.UnitType.ToString() + " is not supported in DoActivity method of [a=" + this.Name + "]");
                }
            }
            return;
        }

        /// <summary>An event handler to call for changing stocking based on prediced pasture biomass</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalMark")]
        private void OnCLEMAnimalMark(object sender, EventArgs e)
        {
            if (this.TimingOK)
            {
                // recalculate numbers and ensure it is not less than number calculated
                int updatedNumberToTag = NumberToTag(); 
                if (updatedNumberToTag < numberToTag)
                    numberToTag = updatedNumberToTag;

                IEnumerable<Ruminant> herd = CurrentHerd(false);
                if (numberToTag > 0)
                {
                    var filterGroups = FindAllChildren<RuminantGroup>();

                    foreach (RuminantGroup item in filterGroups)
                    {
                        foreach (Ruminant ind in item.Filter(herd).Where(a => OverwriteFlag || a.SaleFlag == HerdChangeReason.None).Take(numberToTag))
                        {
                            this.Status = (labourShortfall)?ActivityStatus.Partial:ActivityStatus.Success;

                            ind.SaleFlag = SaleFlagToUse;
                            numberToTag--;
                        }
                    }
                    if(!filterGroups.Any())
                    {
                        foreach (Ruminant ind in herd.Where(a => OverwriteFlag || a.SaleFlag == HerdChangeReason.None).Take(numberToTag))
                        {
                            this.Status = (labourShortfall) ? ActivityStatus.Partial : ActivityStatus.Success;
                            ind.SaleFlag = SaleFlagToUse;
                            numberToTag--;
                        }
                    }
                }
                else
                    this.Status = ActivityStatus.NotNeeded;
            }
            else
                this.Status = ActivityStatus.Ignored;
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
            if (!FindAllChildren<RuminantGroup>().Any())
            {
                string[] memberNames = new string[] { "Specify individuals" };
                results.Add(new ValidationResult($"No individuals have been specified by [f=RuminantGroup] to be marked in [a={Name}]. Provide at least an empty RuminantGroup to mark all individuals.", memberNames));
            }
            return results;
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            return $"\r\n<div class=\"activityentry\">Flag individuals for sale as [{SaleFlagToUse}] in the following groups:</div>";
        } 
        #endregion
    }
}
