using Models.CLEM.Resources;
using Models.Core;
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
    [Description("This activity timer is used to determine whether an activity (and all sub activities) will be performed based on the harvet dates of the CropActivityManageProduct above.")]
    public class ActivityTimerCropHarvest : CLEMModel, IActivityTimer, IValidatableObject, IActivityPerformedNotifier
    {
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Months before harvest to start performing activities
        /// </summary>
        [Description("Months before harvest to start performing activities")]
        [Required, GreaterThanEqual("MonthsBeforeHarvestStop", ErrorMessage = "Months before harvest to start must be greater than or equal to Months before harvest to stop.")]
        public int MonthsBeforeHarvestStart { get; set; }

        /// <summary>
        /// Months before harvest to stop performing activities
        /// </summary>
        [Description("Months before harvest to stop performing activities")]
        [Required]
        public int MonthsBeforeHarvestStop { get; set; }
    
        ///// <summary>
        ///// Invert (NOT in selected range)
        ///// </summary>
        //[Description("Invert (NOT in selected range)")]
  //      [Required]
  //      public bool Invert { get; set; }

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
            // check that this activity has a parent of type CropActivityCollectProduct

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
                string[] memberNames = new string[] { "CropActivityCollectProduct parent" };
                results.Add(new ValidationResult("This crop timer be below a parent of the type Crop Activity Collect Product", memberNames));
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
                int[] range = new int[2] { MonthsBeforeHarvestStart, MonthsBeforeHarvestStop };
                int[] month = new int[2];
                for (int i = 0; i < 2; i++)
                {
                    if(range[i] >= 0)
                    {
                        if(ManageProductActivity.NextHarvest == null)
                        {
                            return false;
                        }
                        month[i] = (ManageProductActivity.NextHarvest.HarvestDate.Year * 100 + ManageProductActivity.NextHarvest.HarvestDate.Month) - range[i];
                    }
                    else
                    {
                        if (ManageProductActivity.PreviousHarvest == null)
                        {
                            return false;
                        }
                        month[i] = (ManageProductActivity.PreviousHarvest.HarvestDate.Year * 100 + ManageProductActivity.PreviousHarvest.HarvestDate.Month) - range[i];
                    }
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
                            Name = this.Name
                        }
                    };
                    this.OnActivityPerformed(activitye);
                }
                return (month[0] <= today && month[1] >= today);
            }

        }

        /// <summary>
        /// Activity has occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnActivityPerformed(EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }
    }
}
