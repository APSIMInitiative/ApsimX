using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.IO;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Defines a group of individual ruminants for which all activities below the implementation consider
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(CLEMRuminantActivityBase))]
    [Description("Specify individuals applied to all activities at or below this point in the simulation tree")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantActivityGroup.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantActivityGroup : FilterGroup<Ruminant>
    {

    }
}
