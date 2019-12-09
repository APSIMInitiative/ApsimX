using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Reporting
{
    /// <summary>
    /// A report class for writing output to the data store.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.HTMLView")]
    [PresenterName("UserInterface.Presenters.LabourAllocationPresenter")]
    [ValidParent(ParentType = typeof(CLEMFolder))]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [Description("This report presents a summary of labour required for all activities.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Reporting/LabourRequirements.htm")]
    public class ReportLabourRequirements: Model
    {
    }
}
