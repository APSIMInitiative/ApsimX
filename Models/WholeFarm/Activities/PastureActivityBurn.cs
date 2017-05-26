using Models.Core;
using Models.WholeFarm.Groupings;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm.Activities
{
	/// <summary>Activity to perform controlled burning of native pastures</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class PastureActivityBurn: WFActivityBase
	{
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
		/// The burn interval (in months, 1 monthly, 12 annual, 24 biennially)
		/// </summary>
		[Description("The burn interval (in months, 12 annual, 24 biennially)")]
		public int BurnInterval { get; set; }

		/// <summary>
		/// Month to perform burn
		/// </summary>
		[Description("Month to perform burn")]
		public int BurnMonth { get; set; }

		///// <summary>
		///// Amount payable
		///// </summary>
		//[Description("Amount payable")]
		//public double Amount { get; set; }

		/// <summary>
		/// Minimum proportion green for fire to carry
		/// </summary>
		[Description("Minimum proportion green for fire to carry")]
		public double MinimumProportionGreen { get; set; }

		/// <summary>
		/// Name of graze food store/paddock to burn
		/// </summary>
		[Description("Name of graze food store/paddock to burn")]
		public string PaddockName { get; set; }

		/// <summary>
		/// Month this overhead is next due.
		/// </summary>
		[XmlIgnore]
		public DateTime NextDueDate { get; set; }

		private GrazeFoodStoreType pasture { get; set; }
		private List<LabourFilterGroupSpecified> labour { get; set; }
		private GreenhouseGasesType methane { get; set; }
		private GreenhouseGasesType nox { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			// check payment interval > 0
			if (BurnInterval <= 0)
			{
				Summary.WriteWarning(this, String.Format("Overhead payment interval must be greater than 1 ({0})", this.Name));
				throw new Exception(String.Format("Invalid payment interval supplied for overhead {0}", this.Name));
			}

			if (BurnMonth >= Clock.StartDate.Month)
			{
				NextDueDate = new DateTime(Clock.StartDate.Year, BurnMonth, Clock.StartDate.Day);
			}
			else
			{
				NextDueDate = new DateTime(Clock.StartDate.Year, BurnMonth, Clock.StartDate.Day);
				while (Clock.StartDate > NextDueDate)
				{
					NextDueDate = NextDueDate.AddMonths(BurnInterval);
				}
			}

			// get pasture
			bool resavailable = false;
			pasture = Resources.GetResourceItem(typeof(GrazeFoodStore), PaddockName, out resavailable) as GrazeFoodStoreType;
			if (!resavailable)
			{
				Summary.WriteWarning(this, String.Format("Could not find pasture in graze food store named \"{0}\" for {1}", PaddockName, this.Name));
				throw new Exception(String.Format("Invalid pasture name ({0}) provided for burn activity {1}", PaddockName, this.Name));
			}

			// get labour specifications
			labour = this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
			if (labour == null) labour = new List<LabourFilterGroupSpecified>();

			bool available;
			methane = Resources.GetResourceItem(typeof(GreenhouseGases), "Methane", out available) as GreenhouseGasesType;
			nox = Resources.GetResourceItem(typeof(GreenhouseGases), "NOx", out available) as GreenhouseGasesType;
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> GetResourcesNeededForActivity()
		{
			ResourceRequestList = null;
			if (this.NextDueDate.Year == Clock.Today.Year & this.NextDueDate.Month == Clock.Today.Month)
			{
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
							ActivityName = this.Name,
							FilterDetails = new List<object>() { item }
						}
						);
					}
				}
			}
			return ResourceRequestList;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void DoActivity()
		{
			// labour is consumed and shortfall has no impact at present
			// could lead to other paddocks burning in future.

			// calculate labour limit
//			double labourLimit = 1;
//			double labourNeeded = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Required);
//			double labourProvided = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Provided);
//			if (labourNeeded > 0)
//			{
//				labourLimit = labourProvided / labourNeeded;
//			}

			// proportion green
			double green = pasture.Pools.Where(a => a.Age < 2).Sum(a => a.Amount);
			double total = pasture.Amount;
			if (total>0)
			{
				if(green / total <= MinimumProportionGreen)
				{
					// TODO add weather to calculate fire intensity
					// TODO calculate patchiness from intensity
					// TODO influence trees and weeds

					// burn
					// remove biomass
					pasture.Remove(new ResourceRequest()
					{
						ActivityName = this.Name,
						Required = total,
						AllowTransmutation = false,
						Reason = "Burn",
						ResourceTypeName = PaddockName,
					}
					);

					// add emissions
					double burnkg = total * 0.76 * 0.46; // burnkg * burning efficiency * carbon content
					if (methane != null)
					{
						//TODO change emissions for green material
						methane.Add(burnkg * 1.333 * 0.0035, "Burn", PaddockName); // * 21; // methane emissions from fire (CO2 eq)
					}
					if (nox != null)
					{
						nox.Add(burnkg * 1.571 * 0.0076 * 0.12, "Burn", PaddockName); // * 21; // methane emissions from fire (CO2 eq)
					}

					// TODO: add fertilisation to pasture for given period.

				}
			}
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

	}
}
