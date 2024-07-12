using DocumentFormat.OpenXml.Presentation;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.DCAPST.Environment;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Intrinsics.X86;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// This stores the parameters relating to RuminantActivityGrowSCA for a ruminant Type
    /// All default values are provided for Bos taurus cattle with Bos indicus values provided as a comment.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyCategorisedView")]
    [PresenterName("UserInterface.Presenters.PropertyCategorisedPresenter")]
    [ValidParent(ParentType = typeof(RuminantParametersHolder))]
    [Description("This model provides all parameters specific to RuminantActivityGrow24")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrow24.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrow24CACRD), typeof(RuminantParametersGrow24CD), typeof(RuminantParametersGrow24CG), typeof(RuminantParametersGrow24CI), typeof(RuminantParametersGrow24CKCL), typeof(RuminantParametersGrow24CM), typeof(RuminantParametersGrow24CP) },
        associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent, ModelAssociationStyle.Child },
        SingleInstance = true)]
    public class RuminantParametersGrow24 : CLEMModel, ISubParameters, ICloneable
    {
        /// <summary>
        /// Create copy of this class
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
