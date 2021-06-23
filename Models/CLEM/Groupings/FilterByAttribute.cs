using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual filter rule based on Attribute exists or value
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantFeedGroupMonthly))]
    [ValidParent(ParentType = typeof(RuminantFeedGroup))]
    [ValidParent(ParentType = typeof(RuminantGroup))]
    [ValidParent(ParentType = typeof(RuminantDestockGroup))]
    [ValidParent(ParentType = typeof(AnimalPriceGroup))]
    // Not yet implemented as Parameter needs to be converterd to GetAttributeByValue(AttributeName)
    //[Description("This ruminant sort rule is used to order results by the value assicated with a named Attribute. Multiple sorts can be chained, with sorts higher in the tree taking precedence.")]
    [Version(1, 0, 0, "")]
    public class FilterByAttribute : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Name of attribute to filter by
        /// </summary>
        [Description("Name of attribute to filter by")]
        [Required]
        public string AttributeName { get; set; }

        /// <summary>
        /// Style to assess attribute
        /// </summary>
        [Description("Means of assessing attribute")]
        [Required]
        public AttributeFilterStyle FilterStyle { get; set; }

        /// <summary>
        /// Name of parameter to filter by
        /// </summary>
        [Description("Operator to use for filtering")]
        [Required]
        public FilterOperators Operator
        {
            get
            {
                return operatr;
            }
            set
            {
                operatr = value;
            }
        }
        private FilterOperators operatr;

        /// <summary>
        /// Value to check for filter
        /// </summary>
        [Description("Value to filter by")]
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }
        private string _value;

        /// <summary>
        /// Convert filter to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string str;
            if (Value == null)
            {
                str = "FILTER NOT DEFINED";
            }
            else
            {
                str = $"Attribute \"{AttributeName}\"";
                if (FilterStyle == AttributeFilterStyle.ByValue)
                {
                    str = $" value ";
                    switch (Operator)
                    {
                        case FilterOperators.Equal:
                            str += "=";
                            break;
                        case FilterOperators.NotEqual:
                            str += "<>";
                            break;
                        case FilterOperators.LessThan:
                            str += "<";
                            break;
                        case FilterOperators.LessThanOrEqual:
                            str += "<=";
                            break;
                        case FilterOperators.GreaterThan:
                            str += ">";
                            break;
                        case FilterOperators.GreaterThanOrEqual:
                            str += ">=";
                            break;
                        default:
                            break;
                    }
                    str += Value;
                }
                else
                {
                    if (Value.ToUpper() == "TRUE" || Value.ToUpper() == "FALSE")
                    {
                        str += ((Operator == FilterOperators.NotEqual && Value.ToUpper() == "TRUE") || (Operator == FilterOperators.Equal && Value.ToUpper() == "FALSE")) ? " does not exist" : " exists";
                    }
                    else
                    {
                        str += " has invalid value for style";
                    }
                }
            }
            return str;
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            if (!this.ValidParent())
            {
                html = "<div class=\"errorlink\">Invalid Parent. RuminantGroup required.</div>";
            }
            if (this.Value == null || this.AttributeName == null)
            {
                html += "<div class=\"errorlink\" style=\"opacity: " + ((this.Enabled) ? "1" : "0.4") + "\">FILTER NOT DEFINED</div>";
            }
            else
            {
                html += "<div class=\"filter\" style=\"opacity: " + ((this.Enabled) ? "1" : "0.4") + "\">" + this.ToString() + "</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "";
        }

        #endregion

        #region validation

        /// <summary>
        /// Validate this component
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // ensure parent is of the right type.
            if (!this.ValidParent())
            {
                string[] memberNames = new string[] { "RuminantFilter" };
                results.Add(new ValidationResult("The FilterByAttribute named " + this.Name + " does not have a valid RuminantGroup parent component", memberNames));
            }
            if ((Value == null || Value == ""))
            {
                string[] memberNames = new string[] { "Value" };
                results.Add(new ValidationResult("Value must be specified", memberNames));
            }
            return results;
        }
        #endregion
    }

}
