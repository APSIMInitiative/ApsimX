using APSIM.Shared.Utilities;
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
    [Version(1, 0, 1, "Supports blank entry for Location to represent 'Not specified - general yards'")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/RuminantFilter.htm")]
    public class RuminantFilter: CLEMModel, IValidatableObject
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

                if (Value.ToUpper() == "TRUE" || Value.ToUpper() == "FALSE")
                {
                    str += ((Operator == FilterOperators.NotEqual && Value.ToUpper() == "TRUE") || (Operator == FilterOperators.Equal && Value.ToUpper() == "FALSE")) ? "Not " : "";
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
                    if (Value.ToString() == "" && Parameter.ToString() == "Location")
                    {
                        str += "Not specified - general yards";
                    }
                    else
                    {
                        str += Value;
                    }
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
            string html = "";
            if(!this.ValidParent())
            {
                html = "<div class=\"errorlink\">Invalid Parent. Ruminant Group type required.</div>";
            }
            if (this.Value == null)
            {
                html += "<div class=\"errorlink\" style=\"opacity: " + ((this.Enabled) ? "1" : "0.4") + "\">[FILTER NOT DEFINED]</div>";
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

        /// <summary>
        /// Validate this component
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // ensure parent is of the right type.
            if(!this.ValidParent())
            {
                string[] memberNames = new string[] { "RuminantFilter" };
                results.Add(new ValidationResult("The RuminantFilter named "+this.Name+" does not have a valid RuminantGroup parent component", memberNames));
            }
            if((Value==null||Value=="")&&(Parameter.ToString()!="Location"))
            {
                string[] memberNames = new string[] { "Value" };
                results.Add(new ValidationResult("Value must be specified", memberNames));
            }
            return results;
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
        /// Is individual a weaner (weaned, but less than 12 months)
        /// </summary>
        Weaner,
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
