using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>Graowing Forage Activity</summary>
	/// <summary>This activity sows, grows and harvests forages.</summary>
	/// <summary>This is done by the values entered by the user as well as looking up the file specified in the 
    /// FileAPSIMForage component in the simulation tree.</summary>
	/// <version>1.0</version>
	/// <updates>First implementation of this activity recreating IAT logic</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class IATGrowCrop: WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;
		[Link]
		Clock Clock = null;
		//[Link]
		//ISummary Summary = null;
        [Link]
        FileAPSIMCrop FileCrop = null;

        /// <summary>
        /// Number for the Climate Region the crop is grown in.
        /// </summary>
        [Description("Climate Region Number")]
        public int Region { get; set; }


        /// <summary>
        /// Name of land type where crop is located
        /// </summary>
        [Description("Land type where crop is located")]
		public string LandTypeNameToUse { get; set; }

		/// <summary>
		/// Name of the crop type to grow
		/// </summary>
		[Description("Name of crop")]
		public string FeedTypeName { get; set; }

        /// <summary>
        /// Percentage of the residue (stover) that is kept
        /// </summary>
        [Description("Proportion of Residue (stover) Kept (%)")]
        public double ResidueKept { get; set; }




        /// <summary>
        /// Area of forage paddock
        /// </summary>
        [XmlIgnore]
		public double Area { get; set; }


		/// <summary>
		/// Area requested
		/// </summary>
		[Description("Area requested")]
		public double AreaRequested { get; set; }


        /// <summary>
        /// Land type
        /// </summary>
        [XmlIgnore]
        public LandType LinkedLandType { get; set; }



        /// <summary>
        /// Human Food type
        /// </summary>
        [XmlIgnore]
        public HumanFoodStoreType LinkedHumanFoodType { get; set; }

        /// <summary>
        /// Feed type
        /// </summary>
        [XmlIgnore]
		public AnimalFoodStoreType LinkedAnimalFoodType { get; set; }

        /// <summary>
        /// Harvest Data retrieved from the Forage File.
        /// </summary>
        [XmlIgnore]
        public List<CropDataType> HarvestData { get; set; }





        private bool gotLandRequested = false; //was this crop able to get the land it requested ?

        /// <summary>
        /// Units of area to use for this run
        /// </summary>
        private UnitsOfAreaType unitsOfArea;


        

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
            //get the units of area for this run from the Land resource.
            unitsOfArea = Resources.Land().UnitsOfArea; 

            // locate Land Type resource for this forage.
            //            bool resourceAvailable = false;
            LinkedLandType = Resources.GetResourceItem(this, typeof(Land), LandTypeNameToUse, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as LandType;
            //if (LinkedLandType == null)
            //{
            //    throw new ApsimXException(this, String.Format("Unable to locate land type {0} in Land for {1}", this.LandTypeNameToUse, this.Name));
            //}

            // locate AnimalFoodStore Type resource for this forage.
            //bool resourceAvailable = false;
            LinkedHumanFoodType = Resources.GetResourceItem(this, typeof(HumanFoodStore), FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as HumanFoodStoreType;
            //if (LinkedHumanFoodType == null)
            //{
            //    throw new ApsimXException(this, String.Format("Unable to locate crop type {0} in HumanFoodStore for {1}", this.FeedTypeName, this.Name));
            //}


            // locate AnimalFoodStore Type resource for this forage.
            //bool resourceAvailable = false;
            LinkedAnimalFoodType = Resources.GetResourceItem(this, typeof(AnimalFoodStore), FeedTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as AnimalFoodStoreType;
			//if (LinkedAnimalFoodType == null)
			//{
   //             throw new ApsimXException(this, String.Format("Unable to locate crop feed type {0} in AnimalFoodStore for {1}", this.FeedTypeName, this.Name));
			//}


            // Retrieve harvest data from the forage file for the entire run. 
            HarvestData = FileCrop.GetCropDataForEntireRun(Region, LinkedLandType.SoilType, FeedTypeName, 
                                                               Clock.StartDate, Clock.EndDate);
            if (HarvestData == null)
            {
                throw new ApsimXException(this, String.Format("Unable to locate in crop file {0} any harvest data for Region {1} , SoilType {2}, CropName {3} between the dates {4} and {5}", 
                    FileCrop.FileName, Region, LinkedLandType.SoilType, FeedTypeName, Clock.StartDate, Clock.EndDate));
            }
            
        }



        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("WFInitialiseActivity")]
        private void OnWFInitialiseActivity(object sender, EventArgs e)
        {

            if (Area == 0 & AreaRequested > 0)
            {
                ResourceRequestList = new List<ResourceRequest>();
                ResourceRequestList.Add(new ResourceRequest()
                {
                    AllowTransmutation = false,
                    Required = AreaRequested * (double)unitsOfArea,
                    ResourceType = typeof(Land),
                    ResourceTypeName = LandTypeNameToUse,
                    ActivityModel = this,
                    Reason = "Assign",
                    FilterDetails = null
                }
                );
            }

            gotLandRequested = TakeResources(ResourceRequestList);


            //Now the Land has been allocated we have an Area 
            if (gotLandRequested)
            {
                //Assign the area actually got after taking it. It might be less than AreaRequested (if partial)
                Area = ResourceRequestList.FirstOrDefault().Available; //TODO: should this be supplied not available ?
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
		/// Method used to perform initialisation of this activity.
		/// This will honour ReportErrorAndStop action but will otherwise be preformed regardless of resources available
		/// It is the responsibility of this activity to determine resources provided.
		/// </summary>
		public override void DoInitialisation()
		{
			return;
		}

		/// <summary>An event handler for a Cut and Carry</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFDoCutAndCarry")]
        private void OnWFDoCutAndCarry(object sender, EventArgs e)
        {
            int year = Clock.Today.Year;
            int month = Clock.Today.Month;

            //nb. we are relying on the fact that the HarvestData list is sorted by date.
            CropDataType nextHarvest = HarvestData.FirstOrDefault();

            if (nextHarvest != null)
            {
                //if this month is a harvest month for this crop
                if ((year == nextHarvest.Year) && (month == nextHarvest.Month))
                {

                    double grain = nextHarvest.GrainWt * Area * (double)unitsOfArea;
                    double stover = nextHarvest.StoverWt * Area * (double)unitsOfArea * (ResidueKept / 100);

					if (grain > 0)
					{
						//TODO: check that there is no N provided with grain
						LinkedHumanFoodType.Add(grain, this.Name, "Harvest");
					}

					if (stover > 0)
					{
						FoodResourcePacket packet = new FoodResourcePacket()
						{
							Amount = stover,
							PercentN = nextHarvest.StoverNpc
						};
						LinkedAnimalFoodType.Add(packet, this.Name, "Harvest");
					}


                    //Now remove the first item from the harvest data list because it has happened.

                    //This causes a problem for the children of this model 
                    //because of a race condition that occurs if user sets MthsBeforeHarvest = 0 
                    //for any of the children of this model. 
                    //Then because this model and children are all executing on the harvest month and 
                    //this executes first and it removes the harvest from the harvest list, 
                    //then the chidren never get the Clock.Today == harvest date (aka costdate).
                    //So to fix this problem, in the children we store the next harvest date (aka costdate) 
                    //in a global variable and don't update its value
                    //until after we have done the Clock.Today == harvest date (aka costdate) comparison.

                    HarvestData.RemoveAt(0);  
                }
            }
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
	}

}
