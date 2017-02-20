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
	public class FodderLimitsFilterGroup: Model
	{
		/// <summary>
		/// Monthly values to supply selected individuals
		/// </summary>
		[Description("Monthly proportion of intake that can come from each pool")]
		public double[] PoolValues { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public FodderLimitsFilterGroup()
		{
			PoolValues = new double[12];
		}

		/// <summary>
		/// Are set limits strict, or can individual continue eating if food available? 
		/// </summary>
		public bool StrictLimits { get; set; }

		/// <summary>An event handler to allow us to initialise ourselves.</summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[EventSubscribe("Commencing")]
		private void OnSimulationCommencing(object sender, EventArgs e)
		{
		}

	}
}
