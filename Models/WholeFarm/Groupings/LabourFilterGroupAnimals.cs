using Models.Core;
using Models.WholeFarm.Activities;
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
	[ValidParent(ParentType = typeof(RuminantActivityBuySell))]
	[ValidParent(ParentType = typeof(RuminantActivityMuster))]
	[ValidParent(ParentType = typeof(RuminantActivityFeed))]
	public class LabourFilterGroupAnimals: LabourFilterGroup
	{
		/// <summary>
		/// Days labour required per x head
		/// </summary>
		[Description("Labour required per x head")]
		public double LabourRequired { get; set; }

		/// <summary>
		/// Number of head per labour unit required
		/// </summary>
		[Description("Number of head per labour unit required")]
		public double LabourHeadUnit { get; set; }

	}
}
