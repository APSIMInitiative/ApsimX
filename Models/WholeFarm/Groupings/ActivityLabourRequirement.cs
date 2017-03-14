using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm.Groupings
{
	/// <summary>labour requirement for an activity</summary>
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(ActivityLabourRequirementGroup))]
	public class ActivityLabourRequirement: WFModel
	{



	}
}
