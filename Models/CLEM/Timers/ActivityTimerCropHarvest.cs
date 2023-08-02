using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// Activity timer based on crop harvest
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityTask))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [Description("This timer is related to the harvest dates of the CropActivityManageProduct above.")]
    [HelpUri(@"Content/Features/Timers/CropHarvest.htm")]
    [Version(1, 0, 3, "Accepts harvest tags for multiple harvests of single crop")]
    [Version(1, 0, 2, "Allows timer sequence to be added as child component")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerCropHarvest : CLEMModel, IActivityTimer, IValidatableObject, IActivityPerformedNotifier
    {
        private CropActivityManageProduct ManageProductActivity;
        private IEnumerable<ActivityTimerSequence> sequenceTimerList;

        /// <summary>
        /// Months before harvest to start performing activities
        /// </summary>
        [Description("Offset from harvest to begin activity (-ve before, 0 harvest, +ve after)")]
        [Required]
        public int OffsetMonthHarvestStart { get; set; }

        /// <summary>
        /// Months before harvest to stop performing activities
        /// </summary>
        [Description("Offset from harvest to end activity (-ve before, 0 harvest, +ve after)")]
        [Required, GreaterThanEqual("OffsetMonthHarvestStart", ErrorMessage = "Offset from harvest to end activity must be greater than or equal to offset to start activity.")]
        public int OffsetMonthHarvestStop { get; set; }

        /// <summary>
        /// Notify CLEM that this activity was performed.
        /// </summary>
        public event EventHandler ActivityPerformed;

        ///<inheritdoc/>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerCropHarvest()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Filter;
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            sequenceTimerList = FindAllChildren<ActivityTimerSequence>();
        }

        /// <summary>
        /// Method to determine whether the activity is due based on harvest details from parent.
        /// </summary>
        /// <returns>Whether the activity is due in the current month</returns>
        public bool ActivityDue
        {
            get
            {
                int? seqStart = null;
                (int? previous, int? first, int? current, int? last) harvestOffsets = ManageProductActivity.HarvestOffset;

                // if next harvest (offset.first) is between bounds
                if (harvestOffsets.first >= OffsetMonthHarvestStart && harvestOffsets.first <= OffsetMonthHarvestStop)
                {
                    seqStart = harvestOffsets.first - OffsetMonthHarvestStart;
                }
                // within period after harvest
                if (harvestOffsets.last > 0 && harvestOffsets.last >= OffsetMonthHarvestStart && harvestOffsets.last <= OffsetMonthHarvestStop)
                {
                    seqStart = harvestOffsets.first - OffsetMonthHarvestStart;
                }
                // if within the period up to next harvest
                if (harvestOffsets.current < 0 && harvestOffsets.current >= OffsetMonthHarvestStart && harvestOffsets.current <= OffsetMonthHarvestStop)
                {
                    seqStart = harvestOffsets.first - OffsetMonthHarvestStart;
                }
                // at time of current harvest
                if (harvestOffsets.current >= OffsetMonthHarvestStart && harvestOffsets.current <= OffsetMonthHarvestStop)
                {
                    seqStart = harvestOffsets.current - OffsetMonthHarvestStart;
                }
                // if withing the period after the previous 
                if (harvestOffsets.previous >= OffsetMonthHarvestStart && harvestOffsets.previous <= OffsetMonthHarvestStop)
                {
                    seqStart = harvestOffsets.first - OffsetMonthHarvestStart;
                }

                if (seqStart != null)
                    return ActivityTimerSequence.IsInSequence(sequenceTimerList, seqStart);

                return false;
            }
        }

        /// <inheritdoc/>
        public bool Check(DateTime dateToCheck)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region validation

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            // check that this activity has a parent of type CropActivityManageProduct

            Model current = this;
            while (current.GetType() != typeof(ZoneCLEM))
            {
                if (current.GetType() == typeof(CropActivityManageProduct))
                {
                    ManageProductActivity = current as CropActivityManageProduct;
                }
                current = current.Parent as Model;
            }

            if (ManageProductActivity == null)
            {
                string[] memberNames = new string[] { "CropActivityManageProduct parent" };
                results.Add(new ValidationResult("This crop timer be below a parent of the type Crop Activity Manage Product", memberNames));
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
                htmlWriter.Write("\r\n<div class=\"filter\">");
                if (OffsetMonthHarvestStart + OffsetMonthHarvestStop == 0)
                {
                    htmlWriter.Write("At harvest</div>");
                }
                else if (OffsetMonthHarvestStop == 0 && OffsetMonthHarvestStart < 0)
                {
                    htmlWriter.Write($"All {CLEMModel.DisplaySummaryValueSnippet(Math.Abs(OffsetMonthHarvestStart))}");
                    htmlWriter.Write(" month" + (Math.Abs(OffsetMonthHarvestStart) == 1 ? "" : "s") + " before harvest (\"first\" if using HarvestType)</div>");
                }
                else if (OffsetMonthHarvestStop > 0 && OffsetMonthHarvestStart == 0)
                {
                    htmlWriter.Write($"All {CLEMModel.DisplaySummaryValueSnippet(Math.Abs(OffsetMonthHarvestStop))}");
                    htmlWriter.Write(" month" + (Math.Abs(OffsetMonthHarvestStop) == 1 ? "" : "s") + " after harvest (\"last\" if using HarvestType)</div>");
                }
                else if (OffsetMonthHarvestStop == OffsetMonthHarvestStart)
                {
                    htmlWriter.Write($"Perform {CLEMModel.DisplaySummaryValueSnippet(Math.Abs(OffsetMonthHarvestStop))}");
                    htmlWriter.Write(" month" + (Math.Abs(OffsetMonthHarvestStart) == 1 ? "" : "s") + " " + ((OffsetMonthHarvestStop < 0) ? "before \"first\" (if using HarvestType)" : "after \"last\" (if using HarvestType)") + " harvest</div>");
                }
                else
                {
                    htmlWriter.Write($"Start {CLEMModel.DisplaySummaryValueSnippet(Math.Abs(OffsetMonthHarvestStart))}");
                    htmlWriter.Write(" month" + (Math.Abs(OffsetMonthHarvestStart) == 1 ? "" : "s") + " ");
                    htmlWriter.Write((OffsetMonthHarvestStart > 0) ? "after \"last\" (if using HarvestType) " : "before \"first\" (if using HarvestType) ");
                    htmlWriter.Write($" harvest and stop {CLEMModel.DisplaySummaryValueSnippet(Math.Abs(OffsetMonthHarvestStop))}");
                    htmlWriter.Write(" month" + (Math.Abs(OffsetMonthHarvestStop) == 1 ? "" : "s") + " ");
                    htmlWriter.Write((OffsetMonthHarvestStop > 0) ? "after \"last\" (if using HarvestType)" : "before \"first\" (if using HarvestType)</div>");
                }
                if (!this.Enabled & !FormatForParentControl)
                    htmlWriter.Write(" - DISABLED!");

                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                {
                    htmlWriter.Write(this.Name);
                }
                htmlWriter.Write($"</div>");
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(FormatForParentControl).ToString() + "\">");
                return htmlWriter.ToString();
            }
        }
        #endregion
    }
}
