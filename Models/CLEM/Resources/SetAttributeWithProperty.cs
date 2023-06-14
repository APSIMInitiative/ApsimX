using DocumentFormat.OpenXml.Drawing.Charts;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Groupings;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Reflection;
using Display = Models.Core.DisplayAttribute;
using System.Linq;

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
    [Description("Specify an attribute for the individual with associated ruminant property value")]
    [HelpUri(@"Content/Features/Resources/SetAttributeWithProperty.htm")]
    [Version(1, 0, 1, "")]
    public class SetAttributeWithProperty : CLEMModel, IValidatableObject, ISetAttribute
    {
        [NonSerialized]
        private PropertyInfo propertyInfo;

        private IEnumerable<string> GetParameters() => (new RuminantGroup()).GetParameterNames().OrderBy(k => k);

        /// <summary>
        /// Store of last instance of the individual attribute defined
        /// </summary>
        private IndividualAttribute lastInstance { get; set; } = null;

        /// <summary>
        /// Name of attribute
        /// </summary>
        [Description("Name of attribute")]
        [Required(AllowEmptyStrings = false)]
        public string AttributeName { get; set; }

        /// <summary>
        /// The property or method to filter by
        /// </summary>
        [Description("Property or method")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Property or method to filter by required")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetParameters), Order = 1)]
        public string PropertyOfIndividual { get; set; }

        /// <summary>
        /// Minumum of value
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Initial value")]
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
        [GreaterThanEqual("Value", ErrorMessage = "Maximum value must be greater than or equal to value")]
        public float MaximumValue { get; set; }

        /// <summary>
        /// Standard deviation as spread when applied to population
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Standard deviation of individuals")]
        [Required]
        public float StandardDeviation { get; set; }

        /// <summary>
        /// Select from tail of normal distribution based on the sign (+ve, -ve) of the standard deviation provided
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(false)]
        [Description("Use s.d. sign to specify distribution tail to use")]
        [Required]
        public bool UseStandardDeviationSign { get; set; } = false;

        /// <summary>
        /// Inheritance style
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Style of inheritance")]
        [Required]
        public AttributeInheritanceStyle InheritanceStyle { get; set; }

        /// <summary>
        /// Genotype variability
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Genotype expression variability (s.d.)")]
        [Required]
        public float GenotypeStandardDeviation { get; set; }

        /// <summary>
        /// Mandatory attribute
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(false)]
        [Description("Mandatory attribute")]
        [Required]
        public bool Mandatory { get; set; }

        /// <summary>
        /// Get the current property infor for accessing and setting values
        /// </summary>
        public PropertyInfo RuminantPropertyInfo { get { return propertyInfo; }  }

        /// <inheritdoc/>
        public IndividualAttribute GetAttribute(bool createNewInstance = true)
        {
            if (createNewInstance || lastInstance is null)
            {
                //double value = Value;
                //double randStdNormal = 0;

                //if (StandardDeviation != 0)
                //{
                //    double u1 = RandomNumberGenerator.Generator.NextDouble();
                //    double u2 = RandomNumberGenerator.Generator.NextDouble();
                //    randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                //                    Math.Sin(2.0 * Math.PI * u2);
                //    if (UseStandardDeviationSign)
                //    {
                //        randStdNormal = Math.Abs(randStdNormal);
                //    }
                //}
                //value = Math.Min(MaximumValue, Math.Max(MinimumValue, value + StandardDeviation * randStdNormal));

                //Single valuef = Convert.ToSingle(value);

                lastInstance = new IndividualAttribute()
                {
                    InheritanceStyle = InheritanceStyle,
                    StoredValue = ApplyVariabilityToAttributeValue(Value, false, UseStandardDeviationSign),
                    SetAttributeSettings = this
                };

                //switch(typeof(Indiv))
            }
            return lastInstance;
        }

        /// <summary>
        /// Provide a modified value of the attribute based on the variability of the attribute
        /// </summary>
        /// <param name="value">The value of the attribute</param>
        /// <param name="useGenotypeSD">Switch to use genotype sd rather than population sd</param>
        /// <param name="allowOneTailedIfSpecfied">Determine whether the one tailed selection from normal distribution is permitted when specfied</param>
        /// <returns>A new value obeying the minimum and maximum limits</returns>
        public float ApplyVariabilityToAttributeValue(float value, bool useGenotypeSD = false, bool allowOneTailedIfSpecfied = false)
        {
            double randStdNormal = 0;
            float sd = (useGenotypeSD) ? GenotypeStandardDeviation : StandardDeviation;

            if (sd != 0)
            {
                double u1 = RandomNumberGenerator.Generator.NextDouble();
                double u2 = RandomNumberGenerator.Generator.NextDouble();
                randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                Math.Sin(2.0 * Math.PI * u2);
                if (!useGenotypeSD & allowOneTailedIfSpecfied & UseStandardDeviationSign)
                {
                    randStdNormal = Math.Abs(randStdNormal);
                }
            }
            return Convert.ToSingle(Math.Min(MaximumValue, Math.Max(MinimumValue, Convert.ToDouble(value) + sd * randStdNormal)));
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SetAttributeWithProperty()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
            SetDefaults();
            propertyInfo = typeof(RuminantType).GetProperty(PropertyOfIndividual);
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
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                if (FormatForParentControl)
                {
                    if (!(CurrentAncestorList.Count >= 3 && CurrentAncestorList[CurrentAncestorList.Count - 3] == typeof(RuminantInitialCohorts).Name))
                    {
                        bool isgroupattribute = (CurrentAncestorList.Count >= 2 && CurrentAncestorList[CurrentAncestorList.Count - 2] == typeof(RuminantInitialCohorts).Name);

                        htmlWriter.Write("\r\n<div class=\"resourcebanneralone clearfix\">");
                        htmlWriter.Write($"Attribute  ");
                        if (AttributeName == null || AttributeName == "")
                            htmlWriter.Write("<span class=\"errorlink\">NOT SET</span>");
                        else
                            htmlWriter.Write($"<span class=\"setvalue\">{AttributeName}</span>");

                        if (StandardDeviation == 0)
                            htmlWriter.Write($" is linked to {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual)} and provided {(isgroupattribute ? "to all cohorts" : "")} with a value of <span class=\"setvalue\">{Value.ToString()}</span> ");
                        else
                            htmlWriter.Write($" is linked to {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual)} and provided {(isgroupattribute ? "to all cohorts" : "")} with a value taken from mean = <span class=\"setvalue\">{Value.ToString()}</span> and s.d. = <span class=\"setvalue\">{StandardDeviation}</span>");

                        htmlWriter.Write($"</div>");
                    }
                }
                else
                {
                    htmlWriter.Write($"\r\n<div class=\"activityentry\">");
                    htmlWriter.Write($"Provide an attribute with the label ");
                    if (AttributeName == null || AttributeName == "")
                        htmlWriter.Write("<span class=\"errorlink\">NOT SET</span>");
                    else
                        htmlWriter.Write($"<span class=\"setvalue\">{AttributeName}</span>");

                    htmlWriter.Write($" that will be inherited with the <span class=\"setvalue\">{InheritanceStyle}</span> style");
                    if (Mandatory)
                        htmlWriter.Write($" and is required by all individuals in the population");

                    htmlWriter.Write($"</div>");

                    htmlWriter.Write($"\r\n<div class=\"activityentry\">");
                    if (StandardDeviation == 0)
                        htmlWriter.Write($"This attribute is linked to {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual)} and has a value of <span class=\"setvalue\">{Value.ToString()}</span> ");
                    else
                        htmlWriter.Write($"This attribute's value is linked to {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual)} is randonly taken from the normal distribution with a mean of <span class=\"setvalue\">{Value.ToString()}</span> and standard deviation of <span class=\"setvalue\">{StandardDeviation}</span> ");

                    if (InheritanceStyle != AttributeInheritanceStyle.None)
                        htmlWriter.Write($" and is allowed to vary between <span class=\"setvalue\">{MinimumValue}</span> and <span class=\"setvalue\">{MaximumValue}</span> when inherited");

                    htmlWriter.Write($"</div>");
                }
                return htmlWriter.ToString();
            }
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags()
        {
            return !FormatForParentControl ? base.ModelSummaryClosingTags() : "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags()
        {
            return !FormatForParentControl ? base.ModelSummaryOpeningTags() : "";
        }

        #endregion


    }
}
