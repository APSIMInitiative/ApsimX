using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// A component to specify an attribute to be applied to initial herd/individual cohort
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantInitialCohorts))]
    [ValidParent(ParentType = typeof(RuminantTypeCohort))]
    [ValidParent(ParentType = typeof(RuminantActivityControlledMating))]
    [Description("This component defines an attribute for the individual")]
    [HelpUri(@"Content/Features/Resources/SetAttributeWithValue.htm")]
    [Version(1, 0, 1, "")]
    public class SetAttributeWithValue : CLEMModel, IValidatableObject, ISetAttribute
    {
        /// <summary>
        /// Name of attribute
        /// </summary>
        [Description("Name of attribute")]
        [Required(AllowEmptyStrings = false)]
        public string AttributeName { get; set; }

        /// <summary>
        /// Attribute value
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Value of attribute")]
        [GreaterThanEqual("Value", ErrorMessage = "Value must be greater than or equal to minimum value")]
        [Required]
        public float Value { get; set; }

        /// <summary>
        /// Minumum of value
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Minumum value")]
        [Required]
        public float MinimumValue { get; set; }

        /// <summary>
        /// Minumum of value
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(100)]
        [Description("Maximum value")]
        [Required]
        [GreaterThanEqual("Value", ErrorMessage ="Maximum value must be greater than or equal to value")]
        public float MaximumValue { get; set; }

        /// <summary>
        /// Standard deviation as spread when applied to population
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Standard deviation of individuals")]
        [Required]
        public float StandardDeviation { get; set; }

        /// <summary>
        /// Inheritance style
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Style of inheritance")]
        [Required]
        public AttributeInheritanceStyle InheritanceStyle { get; set; }

        /// <summary>
        /// Mandatory attribute
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Mandatory attribute")]
        [Required]
        public bool Mandatory { get; set; }

        /// <summary>
        /// Get a random realisation of the set value based on Value and Standard deviation 
        /// </summary>
        public CLEMAttribute GetRandomSetAttribute()
        {
            double value = Value;
            double randStdNormal = 0;

            if (StandardDeviation > 0)
            {
                double u1 = RandomNumberGenerator.Generator.NextDouble();
                double u2 = RandomNumberGenerator.Generator.NextDouble();
                randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                Math.Sin(2.0 * Math.PI * u2);
            }
            value = (float)Math.Min(MaximumValue, Math.Max(MinimumValue, value + StandardDeviation * randStdNormal));

            return new CLEMAttribute()
            {
                InheritanceStyle = InheritanceStyle,
                storedValue = value
            };
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SetAttributeWithValue()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (formatForParentControl)
                {
                    htmlWriter.Write("\r\n<div class=\"resourcebanneralone clearfix\">");
                    htmlWriter.Write($"Attribute  ");
                    if (AttributeName == null || AttributeName == "")
                    {
                        htmlWriter.Write("<span class=\"errorlink\">NOT SET</span>");
                    }
                    else
                    {
                        htmlWriter.Write($"<span class=\"setvalue\">{AttributeName}</span>");
                    }
                    if (StandardDeviation == 0)
                    {
                        htmlWriter.Write($" is provided with a value of <span class=\"setvalue\">{Value.ToString()}</span> ");
                    }
                    else
                    {
                        htmlWriter.Write($" is provided with a value taken from mean = <span class=\"setvalue\">{Value.ToString()}</span> and s.d. = <span class=\"setvalue\">{StandardDeviation}</span>");
                    }
                    htmlWriter.Write($"</div>");
                }
                else
                { 
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">");
                    htmlWriter.Write($"Provide an attribute with the label ");
                    if (AttributeName == null || AttributeName == "")
                    {
                        htmlWriter.Write("<span class=\"errorlink\">NOT SET</span>");
                    }
                    else
                    {
                        htmlWriter.Write($"<span class=\"setvalue\">{AttributeName}</span>");
                    }
                    htmlWriter.Write($" that will be inherited with the <span class=\"setvalue\">{InheritanceStyle.ToString()}</span> style");
                    if (Mandatory)
                    {
                        htmlWriter.Write($" and is required by all individuals in the population");
                    }
                    htmlWriter.Write($"</div>");

                    htmlWriter.Write($"\r\n<div class=\"activityentry\">");
                    if (StandardDeviation == 0)
                    {
                        htmlWriter.Write($"This attribute has a value of <span class=\"setvalue\">{Value.ToString()}</span> ");
                    }
                    else
                    {
                        htmlWriter.Write($"This attribute's value is randonly taken from the normal distribution with a mean of <span class=\"setvalue\">{Value.ToString()}</span> and standard deviation of <span class=\"setvalue\">{StandardDeviation}</span> ");

                    }
                    if (InheritanceStyle != AttributeInheritanceStyle.None)
                    {
                        htmlWriter.Write($" and is allowed to vary between <span class=\"setvalue\">{MinimumValue}</span> and <span class=\"setvalue\">{MaximumValue}</span> when inherited");
                    }
                    htmlWriter.Write($"</div>");
                }
                return htmlWriter.ToString();
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return !formatForParentControl ? base.ModelSummaryClosingTags(true) : "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return !formatForParentControl ? base.ModelSummaryOpeningTags(true) : "";
        }

        #endregion


    }
}
