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
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Individual filter term for to identify individuals. Multiple filters are additive and will further refine the criteria")]
    [ValidParent(ParentType = typeof(LabourFilterGroup))]
    [ValidParent(ParentType = typeof(LabourSpecificationItem))]
    [Version(1, 0, 1, "Adam Liedloff", "CSIRO", "")]
    public class LabourFilter: CLEMModel
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

        /// <summary>
        /// Convert filter to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            string str = "";

            if (Value.ToUpper() == "TRUE" | Value.ToUpper() == "FALSE")
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

        private void UpdateName()
        {
            this.Name = String.Format("Filter[{0}{1}{2}]", Parameter.ToString(), Operator.ToSymbol(), Value );
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            return "<div class=\"filter\">" + this.ToString() + "</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool FormatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool FormatForParentControl)
        {
            return "";
        }
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
        Age
    }

}
