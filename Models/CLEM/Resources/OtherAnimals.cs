using Models.Core;
using Models.Core.Attributes;
using System;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of Ruminant Types.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("Resource group for all other animals types (not ruminants) in the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Other animals/OtherAnimals.htm")]
    [ModelAssociations(associatedModels: new Type[] { typeof(ResourcesHolder) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
    public class OtherAnimals : ResourceBaseWithTransactions
    {

    }
}
