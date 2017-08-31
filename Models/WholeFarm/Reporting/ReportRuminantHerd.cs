using Models.Core;
using Models.WholeFarm.Activities;
using Models.WholeFarm.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.WholeFarm.Reporting
{
	/// <summary>Ruminant reporting</summary>
	/// <summary>This activity writes individual ruminant details for reporting</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(WFActivityBase))]
	[ValidParent(ParentType = typeof(ActivitiesHolder))]
	public class ReportRuminantHerd : Model
	{
		[Link]
		private ResourcesHolder Resources = null;

		/// <summary>
		/// Report item was generated event handler
		/// </summary>
		public event EventHandler OnReportItemGenerated;

		/// <summary>
		/// The details of the summary group for reporting
		/// </summary>
		[XmlIgnore]
		public RuminantReportItemEventArgs ReportDetails { get; set; }

		/// <summary>
		/// Report item generated and ready for reporting 
		/// </summary>
		/// <param name="e"></param>
		protected virtual void ReportItemGenerated(RuminantReportItemEventArgs e)
		{
			if (OnReportItemGenerated != null)
				OnReportItemGenerated(this, e);
		}

		/// <summary>
		/// Function to summarise the herd based on cohorts each month
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("WFHerdSummary")]
		private void OnWFHerdSummary(object sender, EventArgs e)
		{
			RuminantHerd ruminantHerd = Resources.RuminantHerd();
			List<Ruminant> herd = ruminantHerd.Herd;
			ReportDetails = new RuminantReportItemEventArgs();
			foreach (Ruminant item in herd)
			{
				ReportDetails.Individual = item;
				ReportItemGenerated(ReportDetails);
			}
		}
	}

	/// <summary>
	/// New ruminant report item event args
	/// </summary>
	[Serializable]
	public class RuminantReportItemEventArgs : EventArgs
	{
		/// <summary>
		/// Individual ruminant to report
		/// </summary>
		public Ruminant Individual { get; set; }
	}
}