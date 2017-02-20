using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace Models.WholeFarm
{
	/// <summary>Ruminant graze activity</summary>
	/// <summary>This activity determines how a ruminant group will graze</summary>
	/// <summary>It is designed to request food via a food store arbitrator</summary>
	/// <version>1.0</version>
	/// <updates>1.0 First implementation of this activity using NABSA processes</updates>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(Activities))]
	public class RuminantActivityGraze : Model, IFeedActivity
	{
		[Link]
		private Resources Resources = null;
		[Link]
		private Arbitrators Arbitrators = null;
		[Link]
		private Activities Activities = null;

		/// <summary>
		/// Feeding arbitrator to use
		/// </summary>
		[Description("Feeding arbitrator to use")]
		public string FeedArbitratorName { get; set; }

		/// <summary>
		/// Feeding priority (1 high, 10 low)
		/// </summary>
		[Description("Feeding priority")]
		public int FeedPriority { get; set; }

		/// <summary>
		/// Feed type (not used here)
		/// </summary>
		[XmlIgnore]
		public IFeedType FeedType { get; set; }

		/// <summary>
		/// Number of hours grazed
		/// Based on 8 hour grazing days
		/// Could be modified to account for rain/heat walking to water etc.
		/// </summary>
		[Description("Number of hours grazed")]
		public double HoursGrazed { get; set; }

		/// <summary>An event handler to call for all feed requests prior to arbitration and growth</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFRequestFeed")]
		private void OnWFRequestFeed(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;

			IResourceType grazeStoreType = null; 
			if (FeedArbitratorName != "")
			{
				grazeStoreType = Arbitrators.GetByName(FeedArbitratorName) as IResourceType;
			}

			// for each paddock defined in PastureActivityManage
			foreach (PastureActivityManage pasture in Activities.Children.Where(a => a.GetType() == typeof(PastureActivityManage)))
			{
				if (FeedArbitratorName == "")
				{
					grazeStoreType = pasture.FeedType;
				}

				// create feeding activity for this pasture
				RuminantActivityGraze activity = new RuminantActivityGraze();
				activity.FeedPriority = this.FeedPriority;
				activity.FeedType = pasture.FeedType;
				activity.HoursGrazed = this.HoursGrazed;
				activity.Name = String.Format("Graze {0}", pasture.FeedType.Name);

				// calculate kg per ha available
				double kgPerHa = pasture.FeedType.Amount / pasture.Area;

				if (pasture.FeedType.Amount > 0)
				{
					// get list of all Ruminants in this paddock
					foreach (Ruminant ind in herd.Where(a => a.Location == pasture.Name))
					{
						RuminantFeedRequest freqest = new RuminantFeedRequest();
						freqest.FeedActivity = activity;
						freqest.Requestor = ind;

						// Reduce potential intake based on pasture quality for the proportion consumed.


						// calculate intake from potential modified by pasture availability and hours grazed
						freqest.Amount = ind.PotentialIntake * (1 - Math.Exp(-ind.BreedParams.IntakeCoefficientBiomass * kgPerHa)) * (HoursGrazed / 8);
						grazeStoreType.Remove(freqest);
					}
				}
			}
		}
	}

}
