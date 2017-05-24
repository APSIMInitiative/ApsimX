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
	public class IATGrowForage: WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;
		[Link]
		Clock Clock = null;
		//[Link]
		//ISummary Summary = null;
        [Link]
        FileAPSIMForage FileForage = null;



        /// <summary>
        /// Number for the Climate Region the forages are grown in.
        /// </summary>
        [Description("Climate Region Number")]
        public int Region { get; set; }


        /// <summary>
        /// Name of land type where forage is located
        /// </summary>
        [Description("Land type where forage is located")]
		public string LandTypeNameToUse { get; set; }

		/// <summary>
		/// Name of the forage type to grow
		/// </summary>
		[Description("Name of forage")]
		public string FeedTypeName { get; set; }



		/// <summary>
		/// Area of forage paddock
		/// </summary>
		[XmlIgnore]
		public double Area { get; set; }



		/// <summary>
		/// Units of area to use
		/// </summary>
		[Description("units of area")]
		public UnitsOfAreaTypes UnitsOfArea { get; set; }

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
        /// Feed type
        /// </summary>
        [XmlIgnore]
		public AnimalFoodStoreType LinkedAnimalFoodType { get; set; }

        /// <summary>
        /// Harvest Data retrieved from the Forage File.
        /// </summary>
        [XmlIgnore]
        public List<ForageDataType> HarvestData { get; set; }


        /// <summary>
        /// Convert area type specified to hectares
        /// </summary>
        [XmlIgnore]
		public double ConvertToHectares { get
			{
				switch (UnitsOfArea)
				{
					case UnitsOfAreaTypes.Squarekm:
						return 100;
					case UnitsOfAreaTypes.Hectares:
						return 1;
					default:
						return 0;
				}
			}
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{


            //TODO: turn these warnings into fatal errors by throwing exceptions for them.
            //      if you can't find the resource types you need to end the simulation.

            // locate Land Type resource for this forage.
            bool resourceAvailable = false;
            LinkedLandType = Resources.GetResourceItem(typeof(Land), LandTypeNameToUse, out resourceAvailable) as LandType;
            if (LinkedLandType == null)
            {
                throw new ApsimXException(this, String.Format("Unable to locate land type {0} in Land for {1}", this.LandTypeNameToUse, this.Name));
            }



            // locate AnimalFoodStore Type resource for this forage.
            //bool resourceAvailable = false;
			LinkedAnimalFoodType = Resources.GetResourceItem(typeof(AnimalFoodStore), FeedTypeName, out resourceAvailable) as AnimalFoodStoreType;
			if (LinkedAnimalFoodType == null)
			{
                throw new ApsimXException(this, String.Format("Unable to locate forage feed type {0} in AnimalFoodStore for {1}", this.FeedTypeName, this.Name));
			}


            // Retrieve harvest data from the forage file for the entire run. 
            HarvestData = FileForage.GetForageDataForEntireRun(Region, LinkedLandType.SoilType, FeedTypeName, 
                                                               Clock.StartDate, Clock.EndDate);
            if (HarvestData == null)
            {
                throw new ApsimXException(this, String.Format("Unable to locate in forage file {0} any harvest data for Region {1} , SoilType {2}, ForageName {3} between the dates {4} and {5}", 
                    FileForage.FileName, Region, LinkedLandType.SoilType, FeedTypeName, Clock.StartDate, Clock.EndDate));
            }

        }





        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>A list of resource requests</returns>
        public override List<ResourceRequest> DetermineResourcesNeeded()
        {
            //if (Area == 0 & AreaRequested > 0)
            //{
            //    ResourceRequestList = new List<ResourceRequest>();
            //    ResourceRequestList.Add(new ResourceRequest()
            //    {
            //        AllowTransmutation = false,
            //        Required = AreaRequested * ((UnitsOfArea == UnitsOfAreaTypes.Hectares) ? 1 : 100),
            //        ResourceName = "Land",
            //        ResourceTypeName = LandTypeNameToUse,
            //        ActivityName = this.Name,
            //        Reason = "Assign",
            //        FilterDetails = null
            //    }
            //    );
            //    return ResourceRequestList;
            //}
            return null;
        }



        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void PerformActivity()
		{
			return;
		}



        //TODO: turn this into WFDoCutAndCarry event handler instead of start of month
        //      need to do your own resource stuff that is done in the WFActivityBase class.
        //      extract out the GetResourcesRequired() 

        /// <summary>An event handler for Start of Month</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfMonth")]
        private void OnStartOfMonth(object sender, EventArgs e)
        {
            int year = Clock.Today.Year;
            int month = Clock.Today.Month;

            //nb. we are relying on the fact that the HarvestData list is sorted by date.
            ForageDataType nextHarvest = HarvestData.FirstOrDefault();

            if (nextHarvest != null)
            {
                //if this month is a harvest month for this forage
                if ((year == nextHarvest.Year) && (month == nextHarvest.Month))
                {
                    double amount;
                    if (UnitsOfArea == UnitsOfAreaTypes.Squarekm)
                        amount = nextHarvest.Growth * AreaRequested * ConvertToHectares;
                    else
                        amount = nextHarvest.Growth * AreaRequested;


					if (amount > 0)
					{
						FoodResourcePacket packet = new FoodResourcePacket()
						{
							Amount = amount,
							PercentN = nextHarvest.NPerCent
						};
						LinkedAnimalFoodType.Add(amount, this.Name, "Harvest");
					}

                    //now remove the first item from the harvest data list because it has happened
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


    //TODO: sv- don't need this declaration here. It is done already in PastureActivityManage.cs
    //    but sure really move it from there to a common location.
     
	///// <summary>
	///// Types of units of erea to use.
	///// </summary>
	//public enum UnitsOfAreaTypes
	//{
	//	/// <summary>
	//	/// Square km
	//	/// </summary>
	//	Squarekm,
	//	/// <summary>
	//	/// Hectares
	//	/// </summary>
	//	Hectares
	//}
}
