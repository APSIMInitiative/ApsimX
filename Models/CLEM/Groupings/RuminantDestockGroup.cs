using Models.Core;
using Models.CLEM.Activities;
using Models.CLEM.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual ruminants for destocking activities
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(CLEMRuminantActivityBase))]
    [ValidParent(ParentType = typeof(ReportRuminantHerd))]
    [Description("This ruminant filter group selects specific individuals from the ruminant herd using any number of Ruminant Filters. Multiple filters will select groups of individuals required.")]
    public class RuminantDestockGroup : CLEMModel
    {

    }
}