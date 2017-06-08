using Models.Core;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>manage enterprise activity</summary>
	/// <summary>This activity undertakes the overheads of running the enterprise.</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(IATGrowCrop))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    public class IATGrowCropCost : WFActivityBase
	{
        /// <summary>
        /// Get the Clock.
        /// </summary>
        [XmlIgnore]
        [Link]
        Clock Clock = null;

        [XmlIgnore]
        [Link]
        ISummary Summary = null;

        [XmlIgnore]
        [Link]
        private ResourcesHolder Resources = null;

        /// <summary>
        /// Months before harvest to sow crop
        /// </summary>
        [Description("Months before harvest to apply cost")]
        public int MthsBeforeHarvest { get; set; }




        /// <summary>
        /// Cost Per Crop
        /// </summary>
        [Description("Cost - fixed ($)")]
        public double Cost { get; set; }

        /// <summary>
        /// name of account to use
        /// </summary>
        [Description("Name of account to use")]
        public string AccountName { get; set; }


        /// <summary>
        /// Date to apply the cost on.
        /// Has to be stored as a global variable because of a race condition that occurs if user sets  MthsBeforeHarvest=0
        /// Then because ParentGrowCrop and children are all executing on the harvest month and 
        /// the ParentGrowCrop executes first and it removes the harvest from the harvest list, 
        /// then the chidren such as these never get the Clock.Today == harvest date (aka costdate).
        /// So instead we store the next harvest date (aka costdate) in this variable and don't update its value
        /// until after we have done the Clock.Today == harvest date (aka costdate) comparison.
        /// </summary>
        private DateTime costDate;

        /// <summary>
        /// Store finance type to use
        /// </summary>
        private FinanceType bankAccount;

        private IATGrowCrop ParentGrowCrop;




        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            bool foundGrowCrop = FindParentGrowCrop();
            if (! foundGrowCrop)
            {
                Summary.WriteWarning(this, String.Format("Unable to find a parent IATGrowCrop anywhere above ({0}).", this.Name));
                throw new ApsimXException(this, String.Format("Unable to find a parent IATGrowCrop anywhere above ({0}).", this.Name));
            }


            Finance finance = Resources.FinanceResource();
            if (finance != null)
            {
                //bool tmp = true;
                bankAccount = Resources.GetResourceItem(this, typeof(Finance), AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;
                //if (!tmp & AccountName != "")
                //{
                //    Summary.WriteWarning(this, String.Format("Unable to find bank account specified in ({0}).", this.Name));
                //    throw new ApsimXException(this, String.Format("Unable to find bank account specified in ({0}).", this.Name));
                //}
            }

            costDate = CostDateFromHarvestDate();
        }

        /// <summary>
        /// Find a parent of type IATGrowCrop somewhere above this model in the simulation tree.
        /// </summary>
        /// <returns>true or false whether this found it</returns>
        private bool FindParentGrowCrop()
        {

            IModel temp = this.Parent;

            //stop when you hit the top level Activity folder if you have not found it yet.
            while ((temp is ActivitiesHolder) == false)
            {
                //if you have found it.
                if (temp is IATGrowCrop)
                {
                    ParentGrowCrop = (IATGrowCrop)temp;  //set the global variable to it.
                    return true;
                }
                //else go up one more folder level
                temp = temp.Parent;
            }

            return false;
        }



        /// <summary>
        /// Get the cost date from the harvest date.
        /// This will happen every month in case the harvest has occured and there is a new harvest date.
        /// </summary>
        /// <returns></returns>
        private DateTime CostDateFromHarvestDate()
        {
            DateTime nextdate;
            CropDataType nextharvest = ParentGrowCrop.HarvestData.FirstOrDefault();
            if (nextharvest != null)
            {
                nextdate = nextharvest.HarvestDate;
                return nextdate.AddMonths(-1 * MthsBeforeHarvest);
            }
            else
            {
                return new DateTime();
            }
        }



        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
		{
            ResourceRequestList = new List<ResourceRequest>();

            if ((costDate.Year == Clock.Today.Year) && (costDate.Month == Clock.Today.Month))
            {
                string cropName = ParentGrowCrop.FeedTypeName;

                ResourceRequestList.Add(new ResourceRequest()
                {
                    Resource = bankAccount,
                    ResourceType = typeof(Finance),
                    AllowTransmutation = false,
                    Required = this.Cost,
                    ResourceTypeName = this.AccountName,
                    ActivityModel = this,
                    Reason = "Crop cost (fixed) - " + cropName
                }
                );
            }

            costDate = CostDateFromHarvestDate();

            return ResourceRequestList;
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
