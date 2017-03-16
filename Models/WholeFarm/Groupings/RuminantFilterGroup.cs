using Models.Core;
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
	public class RuminantFilterGroup: WFModel
	{
		/// <summary>
		/// Monthly values to supply selected individuals
		/// </summary>
		[Description("Monthly values to supply selected individuals")]
		public double[] MonthlyValues { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public RuminantFilterGroup()
		{
			MonthlyValues = new double[12];
		}
	}
}
