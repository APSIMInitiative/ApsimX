using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	/// <summary>Ruminant predictive stocking activity</summary>
	/// <summary>This activity ensure the total herd size is acceptible to graze the dry season pasture</summary>
	/// <summary>It is designed to consider individuals already marked for sale and add additional individuals before transport and sale.</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using IAT/NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class RuminantActivityPredictiveStocking: Model
	{
		[Link]
		Clock Clock = null;

		/// <summary>
		/// Month for assessing dry season feed requirements
		/// </summary>
		[Description("Month for assessing dry season feed requirements (1-12)")]
		public int AssessmentMonth { get; set; }

		/// <summary>
		/// Number of months to assess
		/// </summary>
		[Description("Number of months to assess")]
		public int DrySeasonLength { get; set; }

		/// <summary>
		/// Minimum estimated feed (kg/ha) allowed at end of period
		/// </summary>
		[Description("Minimum estimated feed (kg/ha) allowed at end of period")]
		public double FeedLowLimit { get; set; }

		/// minimum no that can be sold off... now controlled by sale and transport activity 

		/// <summary>
		/// Minimum breedeer age allowed to be sold
		/// </summary>
		[Description("Minimum breedeer age allowed to be sold")]
		public double MinimumBreederAgeLimit { get; set; }

		/// <summary>
		/// Minimum estimated feed (kg/ha) before restocking
		/// </summary>
		[Description("Minimum estimated feed (kg/ha) before restocking")]
		public double MinimumFeedBeforeRestock { get; set; }

		// restock proportion. I don't understand this.
		// Maximum % restock breeders/age group

		/// <summary>
		/// Allow dry cows to be sold if feed shortage
		/// </summary>
		[Description("Allow dry cows to be sold if feed shortage")]
		public bool SellDryCows { get; set; }

		/// <summary>
		/// Allow wet cows to be sold if feed shortage
		/// </summary>
		[Description("Allow wet cows to be sold if feed shortage")]
		public bool SellWetCows { get; set; }

		/// <summary>
		/// Allow steers to be sold if feed shortage
		/// </summary>
		[Description("Allow steers to be sold if feed shortage")]
		public bool SellSteers { get; set; }

		/// <summary>An event handler to call for all resources other than food for feeding activity</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFAnimalStock")]
		private void OnWFAnimalStock(object sender, EventArgs e)
		{
			if (Clock.Today.Month == AssessmentMonth)
			{

				// calculate dry season pasture available for each managed paddock


				// determine AE marked for sale


				// determine next years prediction


				// adjust herd


				// move to underutilised paddocks

				// buy or sell


			}
		}


	}
}
