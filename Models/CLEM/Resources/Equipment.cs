using Models.Core;
using Models.Core.Attributes;
using System;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of equipment stores.
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("Resource group for all equipment store types (e.g. tractors, bores, harvester) in the simulation")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Equipment/Equipment.htm")]
    [ModelAssociations(associatedModels: new Type[] { typeof(ResourcesHolder) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
    public class Equipment : ResourceBaseWithTransactions
    {

    }
}
