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
    public class ResourceActivityExternalMultiplier : CLEMActivityBase, IValidatableObject
    {
        /// <summary>
        /// Name of the resource this multiplier applies to
        /// </summary>
        [Description("Name of resource")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Resource Type required")]
        [Models.Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(ProductStore), typeof(Equipment), typeof(Finance), typeof(GreenhouseGases), typeof(WaterStore) } })]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Multiplier value
        /// </summary>
        [Description("Multiplier")]
        [GreaterThanEqualValue(0)]
        public double Multiplier { get; set; }

        /// <summary>
        /// Constructor for External multiplier
        /// </summary>
        public ResourceActivityExternalMultiplier()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.Filter;
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            var selectedResourceType = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, ResourceTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);

            if (selectedResourceType is null)
            {
                string[] memberNames = new string[] { "Selected resource" };
                results.Add(new ValidationResult($"Unable to find the resource selected [r={ResourceTypeName}]", memberNames));
            }
            return results;
        }

        #endregion


        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"filter\">");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(ResourceTypeName, "Resource not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write($" x ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(Multiplier, "Not set", HTMLSummaryStyle.Default, warnZero: true));
                htmlWriter.Write("</div>");
                if (!this.Enabled & !FormatForParentControl)
                    htmlWriter.Write(" - DISABLED!");
                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                htmlWriter.Write(this.Name);

                htmlWriter.Write($"</div>");
                htmlWriter.Write($"\r\n<div class=\"filterborder clearfix\" style=\"opacity: {SummaryOpacity(FormatForParentControl)}\">");
                return htmlWriter.ToString();
            }
        }
        #endregion
    }
}
