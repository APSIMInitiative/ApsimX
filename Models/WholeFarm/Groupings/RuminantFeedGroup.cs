using Models.Core;
using Models.WholeFarm.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Groupings
{
	///<summary>
	/// Contains a group of filters to identify individual ruminants
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(RuminantActivityFeed))]
	public class RuminantFeedGroup: WFModel
	{
		/// <summary>
		/// Monthly values to supply selected individuals
		/// </summary>
		[Description("Monthly values to supply selected individuals")]
		public double[] MonthlyValues { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public RuminantFeedGroup()
		{
			MonthlyValues = new double[12];
		}
	}
}
