using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of water stores.
    /// e.g. tap, bore, tank, dam
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all water store types (e.g. tank, dam, bore) for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Water/WaterStore.htm")]
    public class WaterStore : ResourceBaseWithTransactions
    {

    }
}
