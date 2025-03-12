using DocumentFormat.OpenXml.Presentation;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Models.DCAPST.Environment;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
    [Description("This model provides all parameters specific to RuminantActivityGrowPF")]
    [HelpUri(@"Content/Features/Resources/Ruminants/RuminantParametersGrowPF.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(RuminantParametersGrowPFCACRD), typeof(RuminantParametersGrowPFCD), typeof(RuminantParametersGrowPFCG), typeof(RuminantParametersGrowPFCI), typeof(RuminantParametersGrowPFCKCL), typeof(RuminantParametersGrowPFCM), typeof(RuminantParametersGrowPFCP) },
        associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent, ModelAssociationStyle.Descendent, ModelAssociationStyle.Child },
        SingleInstance = true)]
    public class RuminantParametersGrowPF : CLEMModel, ISubParameters, ICloneable
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

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">");
            htmlWriter.Write("Manages all prarameter groups required for RuminantActivityGrowPF</div>");
            return htmlWriter.ToString();
        }

        #endregion

    }


}
