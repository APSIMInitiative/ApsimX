using Models.Core;
using Models.Core.Attributes;
using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This base class for monthly stores of labour information e.g. availability and hire rates.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("")]
    [Version(1, 0, 1, "")]
    public class LabourSpecifications : CLEMModel
    {

    }
}
