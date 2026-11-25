using Models.CLEM.Extras;
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

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            sequenceTimerList = Structure.FindChildren<ActivityTimerSequence>();
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
                (int? previous, int? first, int? current, int? last) = ManageProductActivity.HarvestOffset;

                // if next harvest (offset.first) is between bounds
                if (first >= OffsetMonthHarvestStart && first <= OffsetMonthHarvestStop)
                {
                    seqStart = first - OffsetMonthHarvestStart;
                }
                // within period after harvest
                if (last > 0 && last >= OffsetMonthHarvestStart && last <= OffsetMonthHarvestStop)
                {
                    seqStart = first - OffsetMonthHarvestStart;
                }
                // if within the period up to next harvest
                if (current < 0 && current >= OffsetMonthHarvestStart && current <= OffsetMonthHarvestStop)
                {
                    seqStart = first - OffsetMonthHarvestStart;
                }
                // at time of current harvest
                if (current >= OffsetMonthHarvestStart && current <= OffsetMonthHarvestStop)
                {
                    seqStart = current - OffsetMonthHarvestStart;
                }
                // if withing the period after the previous
                if (previous >= OffsetMonthHarvestStart && previous <= OffsetMonthHarvestStop)
                {
                    seqStart = first - OffsetMonthHarvestStart;
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

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
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
                yield return new ValidationResult("This crop timer be below a parent of the type Crop Activity Manage Product", ["CropActivityManageProduct parent"]);
            }
        }
        #endregion
    }
}
