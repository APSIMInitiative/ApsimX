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
    /// <summary>Add or remove a tag to specified individual ruminants</summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Add or remove a specified tag to/from the specified individuals for customised filtering")]
    [Version(1, 0, 2, "Uses the Attribute feature of Ruminants")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantTag.htm")]

    public class RuminantActivityTag : CLEMRuminantActivityBase, IValidatableObject
    {
        private LabourRequirement labourRequirement;

        /// <summary>
        /// Tag label
        /// </summary>
        [Description("Label of tag")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Label for tag required")]
        public string TagLabel { get; set; }

        /// <summary>
        /// Application style - add or remove tag
        /// </summary>
        [Description("Add or remove tag")]
        [System.ComponentModel.DefaultValueAttribute(TagApplicationStyle.Add)]
        public TagApplicationStyle ApplicationStyle { get; set; }

        private IEnumerable<RuminantGroup> filterGroups;
        private int numberToTag = 0;

        /// <summary>
        /// constructor
        /// </summary>
        public RuminantActivityTag()
        {
            TransactionCategory = "Livestock.Manage";
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
                results.Add(new ValidationResult($"No individuals have been specified by [f=RuminantGroup] for tagging in [a={Name}]. Provide at least an empty RuminantGroup to consider all individuals.", memberNames));
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
            // get all ui tree herd filters that relate to this activity
            this.InitialiseHerd(true, true);
            filterGroups = FindAllChildren<RuminantGroup>();
            // activity is performed in ManageAnimals
            this.AllocationStyle = ResourceAllocationStyle.Manual;
        }

        /// <inheritdoc/>
        [EventSubscribe("CLEMAnimalMark")]
        protected override void PerformActivity(object sender, EventArgs e)
        {
        }

        /// <inheritdoc/>
        public override LabourRequiredArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            IEnumerable<Ruminant> herd = CurrentHerd(false);

            if (filterGroups.Any())
            {
                numberToTag = 0;
                foreach (RuminantGroup item in filterGroups)
                {
                    if (ApplicationStyle == TagApplicationStyle.Add)
                        numberToTag += item.Filter(herd).Where(a => !a.Attributes.Exists(TagLabel)).Count();
                    else
                        numberToTag += item.Filter(herd).Where(a => a.Attributes.Exists(TagLabel)).Count();
                }
            }
            else
                numberToTag = herd.Count();

            double adultEquivalents = herd.Sum(a => a.AdultEquivalent);
            double daysNeeded = 0;
            double numberUnits = 0;
            labourRequirement = requirement;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perHead:
                    numberUnits = numberToTag / requirement.UnitSize;
                    if (requirement.WholeUnitBlocks)
                        numberUnits = Math.Ceiling(numberUnits);

                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new LabourRequiredArgs(daysNeeded, TransactionCategory, this.PredictedHerdName);
        }

        /// <inheritdoc/>
        public override void AdjustResourcesForActivity()
        {
            if (LabourLimitProportion > 0 && LabourLimitProportion < 1 && (labourRequirement != null && labourRequirement.LabourShortfallAffectsActivity))
            {
                this.Status = ActivityStatus.Partial;
                switch (labourRequirement.UnitType)
                {
                    case LabourUnitType.Fixed:
                    case LabourUnitType.perHead:
                        numberToTag = Convert.ToInt32(numberToTag * LabourLimitProportion, CultureInfo.InvariantCulture);
                        break;
                    default:
                        throw new ApsimXException(this, "Labour requirement type " + labourRequirement.UnitType.ToString() + " is not supported in DoActivity method of [a=" + this.Name + "]");
                }
            }
            return;
        }


        /// <inheritdoc/>
        public override void DoActivity()
        {

        }

        /// <summary>An event handler to call for performing all marking for sale, tagging and weaning</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMAnimalMark")]
        private void OnCLEMAnimalMark(object sender, EventArgs e)
        {
            if (this.TimingOK)
            {
                IEnumerable<Ruminant> herd = CurrentHerd(false);
                if (numberToTag > 0)
                {
                    foreach (RuminantGroup item in filterGroups)
                    {
                        foreach (Ruminant ind in item.Filter(GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.AllOnFarm).Where(a => (ApplicationStyle == TagApplicationStyle.Add)? !a.Attributes.Exists(TagLabel): a.Attributes.Exists(TagLabel))).Take(numberToTag))
                        {
                            if(this.Status != ActivityStatus.Partial)
                                this.Status = ActivityStatus.Success;

                            switch (ApplicationStyle)
                            {
                                case TagApplicationStyle.Add:
                                    ind.Attributes.Add(TagLabel);
                                    break;
                                case TagApplicationStyle.Remove:
                                    ind.Attributes.Add(TagLabel);
                                    break;
                            }
                            numberToTag--;
                        }
                    }
                    if(!filterGroups.Any())
                    {
                        foreach (Ruminant ind in GetIndividuals<Ruminant>(GetRuminantHerdSelectionStyle.AllOnFarm).Where(a => (ApplicationStyle == TagApplicationStyle.Add) ? !a.Attributes.Exists(TagLabel) : a.Attributes.Exists(TagLabel)).Take(numberToTag))
                        {
                            if (this.Status != ActivityStatus.Partial)
                                this.Status = ActivityStatus.Success;

                            switch (ApplicationStyle)
                            {
                                case TagApplicationStyle.Add:
                                    ind.Attributes.Add(TagLabel);
                                    break;
                                case TagApplicationStyle.Remove:
                                    ind.Attributes.Add(TagLabel);
                                    break;
                            }
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

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string tagstring = CLEMModel.DisplaySummaryValueSnippet(TagLabel, "Not set");
            return $"\r\n<div class=\"activityentry\">{ApplicationStyle} the tag {tagstring} {((ApplicationStyle == TagApplicationStyle.Add)?"to":"from")} all individuals in the following groups</div>";
        }
        #endregion
    }
}
