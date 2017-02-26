using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm
{
	///<summary>
	/// Contains a group of filters to identify individul ruminants
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public class LabourFilterGroup: Model
	{
		/// <summary>
		/// Days per month selected individuals available
		/// </summary>
		[Description("Days per month selected individuals available")]
		public double[] DaysPerMonth { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public LabourFilterGroup()
		{
			DaysPerMonth = new double[12];
		}

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
		}

	}
}
