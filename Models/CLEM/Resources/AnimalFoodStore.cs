using Models.Core;
using Models.Core.Attributes;
using System;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for all the food designated for animals to eat (eg. Forages and Supplements)
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("Resource group for all animal food store types in the simulation")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/AnimalFoodStore/AnimalFoodStore.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class AnimalFoodStore : ResourceBaseWithTransactions
    {

    }
}
