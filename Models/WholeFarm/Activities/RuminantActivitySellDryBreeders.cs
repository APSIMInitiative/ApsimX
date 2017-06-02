using Models.Core;
using Models.WholeFarm.Resources;
using StdUnits;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Activities
{
	/// <summary>Ruminant dry breeder culling activity</summary>
	/// <summary>This activity provides functionality for kulling dry breeders</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	[ValidParent(ParentType = typeof(ActivityFolder))]
	public class RuminantActivitySellDryBreeders : WFActivityBase
	{
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Name of herd to sell dry breeders
		/// </summary>
		[Description("Name of herd")]
		public string HerdName { get; set; }

		/// <summary>
		/// Determines whether this activity is applied to individuals in a paddock or entire herd
		/// </summary>
		[Description("Apply only to herd in managed paddock")]
		public bool SellFromManagedPaddock { get; set; }

		/// <summary>
		/// Name of paddock to apply sale of dry breeders
		/// </summary>
		[Description("Name of paddock to apply sale of dry breeders")]
		public string PaddockName { get; set; }

		/// <summary>
		/// Minimum conception rate before any selling
		/// </summary>
		[Description("Minimum conception rate before any selling")]
		public double MinimumConceptionBeforeSell { get; set; }

		/// <summary>
		/// Number of months since last birth to be considered dry
		/// </summary>
		[Description("Number of months since last birth to be considered dry")]
		public double MonthsSinceBirth { get; set; }

		/// <summary>
		/// Proportion of dry breeder to sell
		/// </summary>
		[Description("Proportion of dry breeder to sell")]
		public double ProportionToRemove { get; set; }

		/// <summary>An event handler to perform herd dry breeder cull</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalMilking")]
		private void OnWFAnimalMilking(object sender, EventArgs e)
		{
			if (ProportionToRemove > 0)
			{
				// get dry breeders
				RuminantHerd ruminantHerd = Resources.RuminantHerd();
				List<RuminantFemale> herd = ruminantHerd.Herd.Where(a => a.BreedParams.Name == HerdName & a.Gender == Sex.Female).Cast<RuminantFemale>().ToList();
				if (SellFromManagedPaddock)
				{
					// in sepecified paddock
					herd = herd.Where(a => a.Location == PaddockName).ToList();
				}

				// get dry breeders from females
				foreach (RuminantFemale female in herd.Where(a => a.Age - a.AgeAtLastBirth >= MonthsSinceBirth & a.PreviousConceptionRate >= MinimumConceptionBeforeSell & a.AgeAtLastBirth > 0))
				{
					if(WholeFarm.RandomGenerator.NextDouble() <= ProportionToRemove)
					{
						// flag female ready to transport.
						female.SaleFlag = HerdChangeReason.DryBreederSale;
					}					
				}

			}
		}

		/// <summary>
		/// Method to determine resources required for this activity in the current month
		/// </summary>
		/// <returns>List of required resource requests</returns>
		public override List<ResourceRequest> GetResourcesNeededForActivity()
		{
			return null;
		}

		/// <summary>
		/// Method used to perform activity if it can occur as soon as resources are available.
		/// </summary>
		public override void DoActivity()
		{
			return; ;
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
