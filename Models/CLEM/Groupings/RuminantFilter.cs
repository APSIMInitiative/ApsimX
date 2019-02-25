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
    /// Individual filter term for ruminant group of filters to identify individual ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantFeedGroupMonthly))]
    [ValidParent(ParentType = typeof(RuminantFeedGroup))]
    [ValidParent(ParentType = typeof(RuminantFilterGroup))]
    [ValidParent(ParentType = typeof(RuminantDestockGroup))]
    [ValidParent(ParentType = typeof(AnimalPriceGroup))]
    [Description("This ruminant filter rule is used to define specific individuals from the current ruminant herd. Multiple filters are additive.")]
    [Version(1, 0, 1, "")]
    public class RuminantFilter: CLEMModel
    {
        /// <summary>
        /// Name of parameter to filter by
        /// </summary>
        [Description("Name of parameter to filter by")]
        [Required]
        public RuminantFilterParameters Parameter
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
        private RuminantFilterParameters parameter;


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
            if (Value == null)
            {
                str = "FILTER NOT DEFINED";
            }
            else
            {

                if (Value.ToUpper() == "TRUE" | Value.ToUpper() == "FALSE")
                {
                    str += ((Operator == FilterOperators.NotEqual && Value.ToUpper() == "TRUE") | (Operator == FilterOperators.Equal && Value.ToUpper() == "FALSE")) ? "Not " : "";
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
            }
            return str;
        }

        /// <summary>
        /// Create a copy of the current instance
        /// </summary>
        /// <returns></returns>
        public RuminantFilter Clone()
        {
            RuminantFilter clone = new RuminantFilter()
            {
                Parameter = this.Parameter,
                Operator = this.Operator,
                Value = this.Value
            };
            return clone;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            if (this.Value == null)
            {
                return "<div class=\"errorlink\">[FILTER NOT DEFINED]</div>";
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

    }

    /// <summary>
    /// Ruminant filter parameters
    /// </summary>
    public enum RuminantFilterParameters
    {
        /// <summary>
        /// Breed of ruminant
        /// </summary>
        Breed,
        /// <summary>
        /// Herd individuals belong to
        /// </summary>
        HerdName,
        /// <summary>
        /// Gender of individuals
        /// </summary>
        Gender,
        /// <summary>
        /// Age (months) of individuals
        /// </summary>
        Age,
        /// <summary>
        /// ID of individuals
        /// </summary>
        ID,
        /// <summary>
        /// Weight of individuals
        /// </summary>
        Weight,
        /// <summary>
        /// Weight as proportion of High weight achieved
        /// </summary>
        ProportionOfHighWeight,
        /// <summary>
        /// Weight as proportion of Standard Reference Weight
        /// </summary>
        ProportionOfSRW,
        /// <summary>
        /// Current grazing location
        /// </summary>
        Location,
        /// <summary>
        /// Weaned status
        /// </summary>
        Weaned,
        /// <summary>
        /// Is female lactating
        /// </summary>
        IsLactating,
        /// <summary>
        /// Is female pregnant
        /// </summary>
        IsPregnant,
        /// <summary>
        /// Is female a heifer (weaned, >= breed age and weight, no offspring)
        /// </summary>
        IsHeifer,
        /// <summary>
        /// Is male draught individual
        /// </summary>
        Draught,
        /// <summary>
        /// Is male breeding sire
        /// </summary>
        BreedingSire,
    }
}
