using Models.Core.Attributes;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Holds all ruminant parameters sub-models
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantType))]
    [Description("Manages all ruminant parameters")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class RuminantParametersHolder: CLEMModel
    {

    }
}
