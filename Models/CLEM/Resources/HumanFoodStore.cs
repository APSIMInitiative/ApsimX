using System;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for all the food designated for Household to eat (eg. Grain, Tree Crops (nuts) etc.)
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("This resource group holds all human food store types for the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Human food store/HumanFoodStore.htm")]
    public class HumanFoodStore: ResourceBaseWithTransactions
    {

    }

}
