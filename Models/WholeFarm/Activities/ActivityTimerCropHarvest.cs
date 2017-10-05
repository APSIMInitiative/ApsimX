using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
    /// <summary>
    /// Activity timer based on crop harvest
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CropActivityTask))]
    public class ActivityTimerCropHarvest : WFModel, IActivityTimer, IValidatableObject
    {
//		[XmlIgnore]
//		[Link]
//		Clock Clock = null;

        /// <summary>
        /// Months before harvest to start performing activities
        /// </summary>
        [Description("Months before harvest to start performing activities")]
        [Required]
        public int MonthsBeforeHarvestStart { get; set; }

        /// <summary>
        /// Months before harvest to stop performing activities
        /// </summary>
        [Description("Months before harvest to stop performing activities")]
        [Required]
        public int MonthsBeforeHarvestStop { get; set; }
	
		/// <summary>
		/// Invert (NOT in selected range)
		/// </summary>
		[Description("Invert (NOT in selected range)")]
        [Required]
        public bool Invert { get; set; }

        private CropActivityManageProduct collectProductActivity;

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
            while (current.GetType() != typeof(WholeFarm))
            {
                if(current.GetType() == typeof(CropActivityManageProduct))
                {
                    collectProductActivity = current as CropActivityManageProduct;
                }
                current = current.Parent as Model;
            }

            if (collectProductActivity == null)
            {
                string[] memberNames = new string[] { "CropActivityCollectProduct parent" };
                results.Add(new ValidationResult("This crop timer be below a parent of the type Crop Activity Collect Product", memberNames));
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("WFInitialiseActivity")]
        private void OnWFInitialiseActivity(object sender, EventArgs e)
        {
//            endDate = new DateTime(EndDate.Year, EndDate.Month, DateTime.DaysInMonth(EndDate.Year, EndDate.Month));
//			startDate = new DateTime(StartDate.Year, StartDate.Month, 1);
		}

        /// <summary>
        /// Method to determine whether the activity is due
        /// </summary>
        /// <returns>Whether the activity is due in the current month</returns>
        public bool ActivityDue
		{
            get
            {
                //collectProductActivity.Due


                //bool inrange = ((Clock.Today >= startDate) && (Clock.Today <= endDate));
                //if (Invert)
                //{
                //    inrange = !inrange;
                //}
                return true; // inrange;
            }
		}

    }
}
