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
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity adds or removes a specified tag to/from the specified individuals for customised filtering.")]
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

        private int filterGroupsCount = 0;
        private int numberToTag = 0;

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            List<Ruminant> herd = CurrentHerd(false);

            filterGroupsCount = FindAllChildren<RuminantGroup>().Count();
            if (filterGroupsCount > 0)
            {
                numberToTag = 0;
                foreach (RuminantGroup item in FindAllChildren<RuminantGroup>())
                {
                    if (ApplicationStyle == TagApplicationStyle.Add)
                    {
                        numberToTag += herd.Filter(item).Where(a => !a.TagExists(TagLabel)).Count();
                    }
                    else
                    {
                        numberToTag += herd.Filter(item).Where(a => a.TagExists(TagLabel)).Count();
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
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Mark", this.PredictedHerdName);
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            if (LabourLimitProportion > 0 && LabourLimitProportion < 1 && (labourRequirement != null && labourRequirement.LabourShortfallAffectsActivity))
            {
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

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            if (this.TimingOK)
            {
                List<Ruminant> herd = CurrentHerd(false);
                if (numberToTag > 0)
                {
                    foreach (RuminantGroup item in FindAllChildren<RuminantGroup>())
                    {
                        foreach (Ruminant ind in herd.Filter(item).Where(a => (ApplicationStyle == TagApplicationStyle.Add)? !a.TagExists(TagLabel): a.TagExists(TagLabel)).Take(numberToTag))
                        {
                            this.Status = ActivityStatus.Success;
                            switch (ApplicationStyle)
                            {
                                case TagApplicationStyle.Add:
                                    ind.TagAdd(TagLabel);
                                    break;
                                case TagApplicationStyle.Remove:
                                    ind.TagRemove(TagLabel);
                                    break;
                            }
                            numberToTag--;
                        }
                    }
                    if(filterGroupsCount == 0)
                    {
                        foreach (Ruminant ind in herd.Where(a => (ApplicationStyle == TagApplicationStyle.Add) ? !a.TagExists(TagLabel) : a.TagExists(TagLabel)).Take(numberToTag))
                        {
                            this.Status = ActivityStatus.Success;
                            switch (ApplicationStyle)
                            {
                                case TagApplicationStyle.Add:
                                    ind.TagAdd(TagLabel);
                                    break;
                                case TagApplicationStyle.Remove:
                                    ind.TagRemove(TagLabel);
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
