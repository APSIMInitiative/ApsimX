using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to provide a multiplier for specified external resources from resource reader
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourceActivityManageExternal))]
    [Description("Multiply a resource when managed from external sources")]
    [HelpUri(@"Content/Features/Activities/All resources/ManageExternalResourceMultiplier.htm")]
    [Version(1, 0, 1, "")]
    public class ResourceActivityExternalMultiplier : CLEMModel
    {
        /// <summary>
        /// Name of the resource this multiplier applies to
        /// </summary>
        [Description("Name of resource")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Resource Type required")]
        [Models.Core.Display(Type = DisplayType.DropDown, Values = "GetNameOfModelsByType", ValuesArgs = new object[] { new Type[] { typeof(AnimalFoodStoreType), typeof(HumanFoodStoreType), typeof(ProductStoreType) } })]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Multiplier value
        /// </summary>
        [Description("Multiplier")]
        [GreaterThanEqualValue(0)]
        public double Multiplier { get; set; }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"\r\n<div class=\"activityentry\">The amount of all ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(ResourceTypeName, "Resource not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write($" will be multiplied by ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(Multiplier, "Not set", HTMLSummaryStyle.Default));
                htmlWriter.Write("</div>");
                return htmlWriter.ToString();
            }
        }
        #endregion
    }
}
