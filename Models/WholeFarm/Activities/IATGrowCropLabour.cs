using Models.Core;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Activities
{
	/// <summary>Activity to remove the labour used for graowing a crop</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IATGrowCrop))]
    public class IATGrowCropLabour : WFActivityBase
	{

        /// <summary>
        /// Months before harvest to sow crop
        /// </summary>
        [Description("Months before harvest to apply cost")]
        public int MthsBeforeHarvest { get; set; }



        /// <summary>
        /// Parent of this model
        /// </summary>
        private IATGrowCrop parent;

        /// <summary>
        /// Labour settings
        /// </summary>
        private List<LabourFilterGroupSpecified> labour { get; set; }




        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            parent = (IATGrowCrop)this.Parent;

            // get labour specifications
            labour = this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
			if (labour == null) labour = new List<LabourFilterGroupSpecified>();
		}


		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
			ResourceRequestList = null;

            string cropName = parent.FeedTypeName;

            // for each labour item specified
            foreach (var item in labour)
			{
				double daysNeeded = 0;
				switch (item.UnitType)
				{
					case LabourUnitType.Fixed:
						daysNeeded = item.LabourPerUnit;
						break;
					default:
						throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", item.UnitType, item.Name, this.Name));
				}
				if (daysNeeded > 0)
				{
					if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
					ResourceRequestList.Add(new ResourceRequest()
					{
						AllowTransmutation = false,
						Required = daysNeeded,
						ResourceType = typeof(Labour),
						ResourceTypeName = "",
						ActivityModel = this,
						Reason = "Crop labour (fixed) - " + cropName,
						FilterDetails = new List<object>() { item }
					}
					);
				}
			}


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
