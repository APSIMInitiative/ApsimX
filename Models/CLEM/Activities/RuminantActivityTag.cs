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
    [Description("This activity adds or removes a specified tag to/from the specified individuals for customised filtering.")]
    [Version(1, 0, 2, "Uses the Attribute feature of Ruminants")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Ruminant/RuminantTag.htm")]

    public class RuminantActivityTag : CLEMRuminantActivityBase
    {
        private LabourRequirement labourRequirement;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get all ui tree herd filters that relate to this activity
            this.InitialiseHerd(true, true);
            filterGroups = FindAllChildren<RuminantGroup>();
        }

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

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            IEnumerable<Ruminant> herd = CurrentHerd(false);

            if (filterGroups.Any())
            {
                numberToTag = 0;
                foreach (RuminantGroup item in filterGroups)
                {
                    if (ApplicationStyle == TagApplicationStyle.Add)
                    {
                        numberToTag += item.FilterProportion(herd).Where(a => !a.Attributes.Exists(TagLabel)).Count();
                    }
                    else
                    {
                        numberToTag += item.FilterProportion(herd).Where(a => a.Attributes.Exists(TagLabel)).Count();
                    }
                }
            }
            else
            {
                numberToTag = herd.Count();
            }

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
                    {
                        numberUnits = Math.Ceiling(numberUnits);
                    }
                    daysNeeded = numberUnits * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, TransactionCategory, this.PredictedHerdName);
        }

        /// <inheritdoc/>
        public override void AdjustResourcesNeededForActivity()
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
            if (this.TimingOK)
            {
                IEnumerable<Ruminant> herd = CurrentHerd(false);
                if (numberToTag > 0)
                {
                    foreach (RuminantGroup item in filterGroups)
                    {
                        foreach (Ruminant ind in item.FilterProportion(herd).Where(a => (ApplicationStyle == TagApplicationStyle.Add)? !a.Attributes.Exists(TagLabel): a.Attributes.Exists(TagLabel)).Take(numberToTag))
                        {
                            if(this.Status != ActivityStatus.Partial)
                            {
                                this.Status = ActivityStatus.Success;
                            }

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
                        foreach (Ruminant ind in herd.Where(a => (ApplicationStyle == TagApplicationStyle.Add) ? !a.Attributes.Exists(TagLabel) : a.Attributes.Exists(TagLabel)).Take(numberToTag))
                        {
                            if (this.Status != ActivityStatus.Partial)
                            {
                                this.Status = ActivityStatus.Success;
                            }

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
                {
                    this.Status = ActivityStatus.NotNeeded;
                }
            }
            else
            {
                this.Status = ActivityStatus.Ignored;
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <inheritdoc/>
        public override event EventHandler ResourceShortfallOccurred;

        /// <inheritdoc/>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <inheritdoc/>
        public override event EventHandler ActivityPerformed;

        /// <inheritdoc/>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            string tagstring = "";
            if (TagLabel != null && TagLabel != "")
            {
                tagstring = "<span class=\"setvalue\">" + TagLabel + "</span> ";
            }
            else
            {
                tagstring = "<span class=\"errorlink\">[NOT SET]</span> ";
            }
            return $"\r\n<div class=\"activityentry\">{ApplicationStyle} the tag {tagstring} {((ApplicationStyle == TagApplicationStyle.Add)?"to":"from")} all individuals in the following groups</div>";
        }
        #endregion
    }
}
