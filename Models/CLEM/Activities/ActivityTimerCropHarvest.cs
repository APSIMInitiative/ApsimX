using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity timer based on crop harvest
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityTask))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [Description("This activity timer is used to determine whether an activity (and all sub activities) will be performed based on the harvest dates of the CropActivityManageProduct above.")]
    [HelpUri(@"Content/Features/Timers/CropHarvest.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerCropHarvest : CLEMModel, IActivityTimer, IValidatableObject, IActivityPerformedNotifier
    {
        [Link]
        Clock Clock = null;

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
    
        private CropActivityManageProduct ManageProductActivity;

        /// <summary>
        /// Notify CLEM that this activity was performed.
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerCropHarvest()
        {
            this.SetDefaults();
        }

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
                if(current.GetType() == typeof(CropActivityManageProduct))
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
        
        /// <summary>
        /// Method to determine whether the activity is due based on harvest details form parent.
        /// </summary>
        /// <returns>Whether the activity is due in the current month</returns>
        public bool ActivityDue
        {
            get
            {
                int[] range = new int[2] { OffsetMonthHarvestStart, OffsetMonthHarvestStop };
                int[] month = new int[2];

                DateTime harvestDate;

                if (ManageProductActivity.PreviousHarvest != null && OffsetMonthHarvestStop > 0)
                {
                    // compare with previous harvest
                    harvestDate = ManageProductActivity.PreviousHarvest.HarvestDate;
                }
                else if (ManageProductActivity.NextHarvest != null && OffsetMonthHarvestStart <= 0)
                {
                    // compare with next harvest 
                    harvestDate = ManageProductActivity.NextHarvest.HarvestDate;
                }
                else
                {
                    // no harvest to compare with
                    return false;
                }

                for (int i = 0; i < 2; i++)
                {
                    DateTime checkDate = harvestDate.AddMonths(range[i]);
                    month[i] = (checkDate.Year * 100 + checkDate.Month);
                }
                int today = Clock.Today.Year * 100 + Clock.Today.Month;
                if (month[0] <= today && month[1] >= today)
                {
                    // report activity performed.
                    ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
                    {
                        Activity = new BlankActivity()
                        {
                            Status = ActivityStatus.Timer,
                            Name = this.Name,
                        }
                    };
                    activitye.Activity.SetGuID(this.UniqueID);
                    this.OnActivityPerformed(activitye);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Method to determine whether the activity has past based on current dateand harvest details form parent.
        /// </summary>
        /// <returns>Whether the activity is past</returns>
        public bool ActivityPast
        {
            get
            {
                int[] range = new int[2] { OffsetMonthHarvestStart, OffsetMonthHarvestStop };
                int[] month = new int[2];

                DateTime harvestDate;

                if (ManageProductActivity.PreviousHarvest != null & OffsetMonthHarvestStop > 0)
                {
                    // compare with previous harvest
                    harvestDate = ManageProductActivity.PreviousHarvest.HarvestDate;
                }
                else if (ManageProductActivity.NextHarvest != null & OffsetMonthHarvestStart <= 0)
                {
                    // compare with next harvest 
                    harvestDate = ManageProductActivity.NextHarvest.HarvestDate;
                }
                else
                {
                    // no harvest to compare with
                    return false;
                }

                for (int i = 0; i < 2; i++)
                {
                    DateTime checkDate = harvestDate.AddMonths(range[i]);
                    month[i] = (checkDate.Year * 100 + checkDate.Month);
                }
                int today = Clock.Today.Year * 100 + Clock.Today.Month;
                return (month[0] < today && month[1] < today);
            }
        }

        /// <summary>
        /// Method to determine whether the activity is due based on a specified date
        /// </summary>
        /// <returns>Whether the activity is due based on the specified date</returns>
        public bool Check(DateTime dateToCheck)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Activity has occurred 
        /// </summary>
        /// <param name="e"></param>
        public virtual void OnActivityPerformed(EventArgs e)
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
            if (OffsetMonthHarvestStart + OffsetMonthHarvestStop == 0)
            {
                html += "\n<div class=\"filter\">At harvest";
                html += "\n</div>";
            }
            else if (OffsetMonthHarvestStop == 0 && OffsetMonthHarvestStart < 0)
            {
                html += "\n<div class=\"filter\">";
                html += "All <span class=\"setvalueextra\">";
                html += Math.Abs(OffsetMonthHarvestStart).ToString() + "</span> month" + (Math.Abs(OffsetMonthHarvestStart) == 1 ? "" : "s") + " before harvest";
                html += "</div>";
            }
            else if (OffsetMonthHarvestStop > 0 && OffsetMonthHarvestStart == 0)
            {
                html += "\n<div class=\"filter\">";
                html += "All <span class=\"setvalueextra\">";
                html += OffsetMonthHarvestStop.ToString() + "</span> month" + (Math.Abs(OffsetMonthHarvestStop) == 1 ? "" : "s") + " after harvest";
                html += "</div>";
            }
            else if (OffsetMonthHarvestStop == OffsetMonthHarvestStart)
            {
                html += "\n<div class=\"filter\">";
                html += "Perform <span class=\"setvalueextra\">";
                html += Math.Abs(OffsetMonthHarvestStop).ToString() + "</span> month" + (Math.Abs(OffsetMonthHarvestStart) == 1 ? "" : "s") + " "+((OffsetMonthHarvestStop<0)?"before":"after")+" harvest";
                html += "</div>";
            }
            else
            {
                html += "\n<div class=\"filter\">";
                html += "Start <span class=\"setvalueextra\">";
                html += Math.Abs(OffsetMonthHarvestStart).ToString() + "</span> month"+(Math.Abs(OffsetMonthHarvestStart)==1?"":"s") +" ";
                html += (OffsetMonthHarvestStart > 0) ? "after " : "before ";
                html += " harvest and stop <span class=\"setvalueextra\">";
                html += Math.Abs(OffsetMonthHarvestStop).ToString() + "</span> month" + (Math.Abs(OffsetMonthHarvestStop) == 1 ? "" : "s") + " ";
                html += (OffsetMonthHarvestStop > 0) ? "after " : "before ";
                html += "</div>";
            }
            if (!this.Enabled)
            {
                html += " - DISABLED!";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + "\">";
            return html;
        }
    }
}
