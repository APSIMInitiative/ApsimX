using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm
{
	///<summary>
	/// Resource transmutation
	/// Will convert one resource into another (e.g. $ => labour) 
	/// These re defined under each ResourceType in the Resources section of the UI tree
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(IResourceType))]
	public class Transmutation: WFModel
	{
		/// <summary>
		/// Amount of this resource per unit purchased
		/// </summary>
		[Description("Amount of this resource per unit purchased")]
		public double AmountPerUnitPurchase { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
			if (Children.Where(a => a.GetType() == typeof(TransmutationCost)).Count()==0)
			{
				throw new Exception("Invalid costs povided in Transmutation:"+this.Name+" for ResourceType:"+this.Parent.Name);
			}
		}
	}

	///<summary>
	/// Resource transmutation cost item
	/// Determines the amount of resource required for the transmutation
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Transmutation))]
	public class TransmutationCost : WFModel
	{
		/// <summary>
		/// Name of resource to use
		/// </summary>
		[Description("Name of Resource to use")]
		public string ResourceName { get; set; }

		/// <summary>
		/// Name of resource type to use
		/// </summary>
		[Description("Name of Resource Type to use")]
		public string ResourceTypeName { get; set; }

		/// <summary>
		/// Cost of transmutation
		/// </summary>
		[Description("Cost per unit")]
		public double CostPerUnit { get; set; }
	}


}
