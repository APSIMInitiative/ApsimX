using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.WholeFarm.Groupings
{
	///<summary>
	/// Contains a group of filters to identify individul ruminants
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	public class LabourFilterGroupDefine: WFModel
	{
		/// <summary>
		/// Days per month selected individuals available
		/// </summary>
		[Description("Days per month selected individuals available")]
		public double[] DaysPerMonth { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public LabourFilterGroupDefine()
		{
			DaysPerMonth = new double[12];
		}

	}
}
