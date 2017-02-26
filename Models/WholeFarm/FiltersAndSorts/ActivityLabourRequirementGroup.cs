using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.WholeFarm
{   ///<summary>
	/// Contains a group of filters to identify individul ruminants
	///</summary> 
	[Serializable]
	[ViewName("UserInterface.Views.GridView")]
	[PresenterName("UserInterface.Presenters.PropertyPresenter")]
	[ValidParent(ParentType = typeof(ActivityLabourRequirement))]
	public class ActivityLabourRequirementGroup: Model
	{
		/// <summary>
		/// Days required in this time step
		/// </summary>
		[Description("Days required")]
		public double DaysRequired { get; set; }
	}
}
