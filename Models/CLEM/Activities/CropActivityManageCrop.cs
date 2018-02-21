using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>Grow management activity</summary>
    /// <summary>This activity sets aside land for the crop</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages a crop by assigning land to be used for child activities.")]
    public class CropActivityManageCrop: CLEMActivityBase, IValidatableObject
    {
        /// <summary>
        /// Name of land type where crop is located
        /// </summary>
        [Description("Land type where crop is to be grown")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of land resource type required")]
        public string LandItemNameToUse { get; set; }

        /// <summary>
        /// Area of land requested
        /// </summary>
        [Description("Area of crop")]
        [Required, GreaterThanValue(0)]
        public double AreaRequested { get; set; }

        /// <summary>
        /// Use entire area available
        /// </summary>
        [Description("Use entire area available")]
        public bool UseAreaAvailable { get; set; }
        
        /// <summary>
        /// Area of land actually received (maybe less than requested)
        /// </summary>
        [XmlIgnore]
        public double Area;

        /// <summary>
        /// Land item
        /// </summary>
        [XmlIgnore]
        public LandType LinkedLandItem { get; set; }

        private bool gotLandRequested = false; //was this crop able to get the land it requested ?

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            // check that this activity contains at least one CollectProduct activity
            if(this.Children.Where(a => a.GetType() == typeof(CropActivityManageProduct)).Count() == 0)
            {
                string[] memberNames = new string[] { "Collect product activity" };
                results.Add(new ValidationResult("At least one CropActivityCollectProduct activity must be present under this manage crop activity", memberNames));
            }
            return results;
        }

        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate Land Type resource for this forage.
            LinkedLandItem = Resources.GetResourceItem(this, typeof(Land), LandItemNameToUse, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as LandType;

            // used to check for Area = 0 and AreaRequested > 0. these are now validated and Area cannot be set before now.
            ResourceRequestList = new List<ResourceRequest>
            {
                new ResourceRequest()
                {
                    AllowTransmutation = false,
                    Required = UseAreaAvailable ? LinkedLandItem.AreaAvailable : AreaRequested,
                    ResourceType = typeof(Land),
                    ResourceTypeName = LandItemNameToUse,
                    ActivityModel = this,
                    Reason = "Assign",
                    FilterDetails = null
                }
            };

            gotLandRequested = TakeResources(ResourceRequestList, false);

            //Now the Land has been allocated we have an Area 
            if (gotLandRequested)
            {
                //Assign the area actually got after taking it. It might be less than AreaRequested (if partial)
                Area = ResourceRequestList.FirstOrDefault().Provided;
            }
        }


        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>A list of resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
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
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
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
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }

    }
}
