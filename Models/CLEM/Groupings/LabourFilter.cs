using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual filter term for to identify individuals
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Individual filter term for to identify individuals. Multiple filters are additive and will further refine the criteria")]
    [ValidParent(ParentType = typeof(LabourFilterGroup))]
    [ValidParent(ParentType = typeof(LabourSpecificationItem))]
    [ValidParent(ParentType = typeof(LabourPriceGroup))]
    [ValidParent(ParentType = typeof(LabourFeedGroup))]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/LabourFilter.htm")]
    public class LabourFilter: CLEMModel, IFilter
    {
        /// <summary>
        /// Name of parameter to filter by
        /// </summary>
        [Description("Name of parameter to filter by")]
        [Required]
        public LabourFilterParameters Parameter
        {
            get
            {
                return parameter;
            }
            set
            {
                parameter = value;
            }
        }
        private LabourFilterParameters parameter;

        /// <summary>
        /// Operator to filter with
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
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value to filter by required")]
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

        /// <inheritdoc/>
        public string ParameterName => Parameter.ToString();

        /// <summary>
        /// Convert filter to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string str = "";
            if (Value == null)
            {
                return str;
            }

            if (Value.ToUpper() == "TRUE" || Value.ToUpper() == "FALSE")
            {
                str += (Operator == FilterOperators.NotEqual) ? "Not " : "";
                str += Parameter;
            }
            else
            {
                str += Parameter;
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
            if (this.Value == null)
            {
                return "<div class=\"filtererror\">No value provided</div>";
            }
            else
            {
                return "<div class=\"filter\">" + this.ToString() + "</div>";
            }
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
    }

    /// <summary>
    /// Ruminant filter parameters
    /// </summary>
    public enum LabourFilterParameters
    {
        /// <summary>
        /// Name of individual
        /// </summary>
        Name,
        /// <summary>
        /// Gender of individuals
        /// </summary>
        Gender,
        /// <summary>
        /// Age (months) of individuals
        /// </summary>
        Age,
        /// <summary>
        /// Is hired labour
        /// </summary>
        Hired
    }

}
