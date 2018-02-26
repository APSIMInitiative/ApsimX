using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>Grow a crop activity</summary>
    /// <summary>This activity sows, grows and harvests crops.</summary>
    /// <version>1.0</version>
    /// <updates>First implementation of this activity recreating IAT logic</updates>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    public class IATCropLand: CLEMActivityBase
    {
        /// <summary>
        /// Name of land type where crop is located
        /// </summary>
        [Description("Land item where crop is to be grown")]
        [Required]
        public string LandItemNameToUse { get; set; }

        /// <summary>
        /// Area of land requested
        /// </summary>
        [Description("Area requested")]
        [Required]
        public double AreaRequested { get; set; }

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

        ///// <summary>
        ///// Units of area to use for this run
        ///// </summary>
        //private UnitsOfAreaType unitsOfArea;


        

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {

            //get the units of area for this run from the Land resource.
            //unitsOfArea = Resources.Land().UnitsOfArea; 

            // locate Land Type resource for this forage.
            LinkedLandItem = Resources.GetResourceItem(this, typeof(Land), LandItemNameToUse, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as LandType;            
            
        }



        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {

            if (Area == 0 & AreaRequested > 0)
            {
                ResourceRequestList = new List<ResourceRequest>();
                ResourceRequestList.Add(new ResourceRequest()
                {
                    AllowTransmutation = false,
                    Required = AreaRequested, // * (double)unitsOfArea,
                    ResourceType = typeof(Land),
                    ResourceTypeName = LandItemNameToUse,
                    ActivityModel = this,
                    Reason = "Assign",
                    FilterDetails = null
                }
                );
            }

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
