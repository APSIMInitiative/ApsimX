using Models.Core;
using Models.WholeFarm.Activities;
using Models.WholeFarm.Reporting;
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
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(WFActivityBase))]
    [ValidParent(ParentType = typeof(WFRuminantActivityBase))]
    [ValidParent(ParentType = typeof(ReportRuminantHerd))]

    public class RuminantFilterGroup : WFModel
	{

	}
}