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
	[ValidParent(ParentType = typeof(RuminantActivityHerdCost))]
	[ValidParent(ParentType = typeof(RuminantActivityMilking))]
	[ValidParent(ParentType = typeof(ControlledMatingSettings))]
	[ValidParent(ParentType = typeof(OtherAnimalsActivityBreed))]
	[ValidParent(ParentType = typeof(OtherAnimalsActivityFeed))]
	public class LabourFilterGroupSpecified: LabourFilterGroup
	{
		/// <summary>
		/// Labour unit type
		/// </summary>
		[Description("Labour unit")]
		public LabourUnitType UnitType { get; set; }

		/// <summary>
		/// Size of unit
		/// </summary>
		[Description("Size of unit")]
		public double UnitSize { get; set; }

		/// <summary>
		/// Days labour required per unit or fixed (days)
		/// </summary>
		[Description("Days labour required per unit or fixed (days)")]
		public double LabourPerUnit { get; set; }

	}
}
