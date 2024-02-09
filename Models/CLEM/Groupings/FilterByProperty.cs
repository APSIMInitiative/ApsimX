using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Display = Models.Core.DisplayAttribute;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Filter using property or method (without arguments) of the IFilterable individual
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Defines a filter rule using properties and methods of the individual")]
    [ValidParent(ParentType = typeof(IFilterGroup))]
    [Version(1, 0, 0, "")]
    [HelpUri(@"Content/Features/Filters/FilterByProperty.htm")]

    public class FilterByProperty : Filter, IValidatableObject
    {
        [NonSerialized]
        private PropertyInfo propertyInfo;
        private bool validOperator = true;
        
        private IEnumerable<string> GetParameters() => Parent?.GetParameterNames().OrderBy(k => k);

        /// <summary>
        /// The property or method to filter by
        /// </summary>
        [Description("Property or method")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Property or method to filter by required")]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetParameters), Order = 1)]
        public string PropertyOfIndividual { get; set; }

        /// <inheritdoc/>
        public override object ModifiedValueToUse
        {
            get 
            {
                switch (PropertyOfIndividual)
                {
                    // allow full path names for location by ignoring the GrazeFoodStore component.
                    case "Location":
                        if(Value is not null &&  Value.ToString().Contains("."))
                            return Value.ToString().Split('.')[1];
                        break;
                }
                return Value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public FilterByProperty()
        {
            base.SetDefaults();
        }

        ///<inheritdoc/>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            List<ValidationResult> results = new List<ValidationResult>();
            ValidationContext context = new ValidationContext(this, null, null);
            if (Validator.TryValidateObject(this, context, results, true))
            {
                Initialise();
                // rules can only be built on commence not during use in UI (Descriptive summaries)
                BuildRule();
            }
        }

        /// <inheritdoc/>
        public override void Initialise()
        {
            if (PropertyOfIndividual != null && PropertyOfIndividual != "")
            {
                propertyInfo = Parent.GetProperty(PropertyOfIndividual);
                validOperator = CheckValidOperator(propertyInfo, out string _);
            }
        }

        /// <inheritdoc/>
        public override void BuildRule()
        {
            if (Rule is null)
                Rule = Compile<IFilterable>();
        }

        /// <inheritdoc/>
        public override Func<T, bool> Compile<T>()
        {
            if (!validOperator || propertyInfo is null) return f => false;
            return CompileComplex<T>();
        }

        private Func<T, bool> CompileComplex<T>()
        {
            // Check that the filter applies to objects of type T
            var filterParam = Expression.Parameter(typeof(T));

            // check if the parameter passes can inherit the declaring type
            // this will not allow females to check male properties
            // convert parameter to the type of the property, null if fails
            var filterInherit = Expression.TypeAs(filterParam, propertyInfo.DeclaringType);
            var typeis = Expression.TypeIs(filterParam, propertyInfo.DeclaringType);

            // Look for the property
            var key = Expression.Property(filterInherit, propertyInfo.Name);

            // Try convert the Value into the same data type as the property

            string propError = "";
            switch (propertyInfo.PropertyType.Name)
            {
                case "Boolean":
                    if(Value != null && !bool.TryParse(Value.ToString(), out _))
                        propError = $"The value to compare [{Value}] provided for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}] is not valid for the property type [Boolean]{System.Environment.NewLine}Valid entries are [True, true, False, false, 1, 0]";
                    break;
                default:
                    break;
            }
            if(propError != "")
            {
                Warnings.CheckAndWrite(propError, Summary, this, MessageType.Error);
                throw new ApsimXException(this, propError);
            }

            var ce = propertyInfo.PropertyType.IsEnum ? Enum.Parse(propertyInfo.PropertyType, ModifiedValueToUse.ToString(), true) : Convert.ChangeType(ModifiedValueToUse ?? 0, propertyInfo.PropertyType);
            var value = Expression.Constant(ce);

            // Create a lambda that compares the filter value to the property on T
            // using the provided operator
            Expression binary;
            if (Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse)
            {
                // Allow for IsTrue and IsFalse operator
                ce = (Operator == ExpressionType.IsTrue);
                binary = Expression.MakeBinary(ExpressionType.Equal, key, Expression.Constant(ce));
            }
            else
                binary = Expression.MakeBinary(Operator, key, value);

            // only perfom if the type is a match to the type of property
            var body = Expression.Condition(
                typeis,
                binary,
                Expression.Constant(false)
                );

            var lambda = Expression.Lambda<Func<T, bool>>(body, filterParam).Compile();
            return lambda;
        }

        /// <summary>
        /// Check if the specified operator is valid for the selected property
        /// </summary>
        /// <param name="property">PropertyInfo of the property</param>
        /// <param name="errorMessage">Error message returned for reporting</param>
        /// <returns>True if operator is valid</returns>
        public bool CheckValidOperator(PropertyInfo property, out string errorMessage)
        {
            errorMessage = "";
            switch (property.PropertyType.IsEnum?"enum":property.PropertyType.Name)
            {
                case "Boolean":
                    switch (Operator)
                    {
                        case ExpressionType.Equal:
                        case ExpressionType.NotEqual:
                        case ExpressionType.IsTrue:
                        case ExpressionType.IsFalse:
                            break;
                        case ExpressionType.GreaterThan:
                        case ExpressionType.GreaterThanOrEqual:
                        case ExpressionType.LessThan:
                        case ExpressionType.LessThanOrEqual:
                            errorMessage = $"Invalid operator of type [{OperatorToSymbol()}] for [{property.PropertyType.Name}] property [{property.Name}] in [f={this.NameWithParent}] ";
                            return false;
                        default:
                            errorMessage = $"Unsupported operator of type [{Operator}] for [{property.PropertyType.Name}] property [{property.Name}] in [f={this.NameWithParent}] ";
                            return false;
                    };
                    break;
                case "enum":
                case "String":
                    switch (Operator)
                    {
                        case ExpressionType.Equal:
                        case ExpressionType.NotEqual:
                            break;
                        case ExpressionType.IsTrue:
                        case ExpressionType.IsFalse:
                        case ExpressionType.GreaterThan:
                        case ExpressionType.GreaterThanOrEqual:
                        case ExpressionType.LessThan:
                        case ExpressionType.LessThanOrEqual:
                            errorMessage = $"Invalid operator of type [{OperatorToSymbol()}] for [{property.PropertyType.Name}] property [{property.Name}] in [f={this.NameWithParent}] ";
                            return false;
                        default:
                            errorMessage = $"Unsupported operator of type [{Operator}] for [{property.PropertyType.Name}] property [{property.Name}] in [f={this.NameWithParent}] ";
                            return false;
                    };
                    break;
                case "Int32":
                case "Double":
                    switch (Operator)
                    {
                        case ExpressionType.IsFalse:
                        case ExpressionType.IsTrue:
                            errorMessage = $"Invalid operator of type [{OperatorToSymbol()}] for [{property.PropertyType.Name}] property [{property.Name}] in [f={this.NameWithParent}] ";
                            return false;
                        case ExpressionType.Equal:
                        case ExpressionType.NotEqual:
                        case ExpressionType.GreaterThan:
                        case ExpressionType.GreaterThanOrEqual:
                        case ExpressionType.LessThan:
                        case ExpressionType.LessThanOrEqual:
                            break;
                        default:
                            errorMessage = $"Unsupported operator of type [{Operator}] for [{property.PropertyType.Name}] property [{property.Name}] in [f={this.NameWithParent}] ";
                            return false;
                    };
                    break;
                default:
                    errorMessage = $"Unsupported property type [{property.PropertyType.Name}] for property [{property.Name}] in [f={this.NameWithParent}] ";
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Convert filter to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return FilterString(false);
        }

        /// <summary>
        /// Convert filter to html string
        /// </summary>
        /// <returns></returns>
        public string ToHTMLString()
        {
            return FilterString(true);
        }

        private string FilterString(bool htmltags)
        {
            Initialise();

            using (StringWriter filterWriter = new StringWriter())
            {
                if (propertyInfo is null)
                {
                    filterWriter.Write($"Filter:");
                    string errorlink = (htmltags) ? " <span class=\"errorlink\">" : " ";
                    string spanclose = (htmltags) ? "</span>" : "";
                    string message = (PropertyOfIndividual == null || PropertyOfIndividual == "") ? "Not Set" : $"Unknown: {PropertyOfIndividual}";
                    filterWriter.Write($"{errorlink}{message}{spanclose}");
                    return filterWriter.ToString();
                }

                filterWriter.Write($"Filter:");
                bool truefalse = IsOperatorTrueFalseTest();
                if (truefalse | (propertyInfo != null && propertyInfo.PropertyType.IsEnum))
                {
                    if (propertyInfo.PropertyType == typeof(bool))
                    {
                        if (Operator == ExpressionType.IsFalse || Value?.ToString().ToLower() == "false")
                            filterWriter.Write(" not");
                        filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                    }
                    else
                    {
                        filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                        if (validOperator)
                            filterWriter.Write((Operator == ExpressionType.IsFalse || Value?.ToString().ToLower() == "false") ? " not" : " is");
                        else
                        {
                            string errorlink = (htmltags) ? "<span class=\"errorlink\">" : "";
                            string spanclose = (htmltags) ? "</span>" : "";
                            filterWriter.Write($"{errorlink}invalid operator {OperatorToSymbol()}{spanclose}");
                        }
                        filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(Value?.ToString(), "No value", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                    }
                }
                else
                {
                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");

                    if (propertyInfo != null)
                    {
                        if (validOperator)
                            filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                        else
                        {
                            string errorlink = (htmltags) ? "<span class=\"errorlink\">" : "";
                            string spanclose = (htmltags) ? "</span>" : "";
                            filterWriter.Write($"{errorlink}invalid operator {OperatorToSymbol()}{propertyInfo.PropertyType.Name}{spanclose}");
                        }
                    }
                    else
                        filterWriter.Write($" {DisplaySummaryValueSnippet(OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");

                    filterWriter.Write($" {DisplaySummaryValueSnippet(Value?.ToString(), "No value", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                }
                return filterWriter.ToString();
            }
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // no property set, so no need to continue this validation as empty property checked by attribute
            if (PropertyOfIndividual is null || PropertyOfIndividual == "")
                return results;

            if((Value is null || Value.ToString() == "") & !(Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse))
            {
                string[] memberNames = new string[] { "Missing filter compare value" };
                results.Add(new ValidationResult($"A value to compare with the Property [{PropertyOfIndividual}] is required for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}]", memberNames));
            }

            // check valid operator
            if(!CheckValidOperator(propertyInfo, out _))
            {
                string[] memberNames = new string[] { "Invalid operator" };
                results.Add(new ValidationResult($"The operator provided for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}] is not valid for the property type [{propertyInfo.Name}]", memberNames));
            }

            // check valid property value.
            // valid for enum
            if (propertyInfo.PropertyType.IsEnum)
            {
                if(!Enum.TryParse(propertyInfo.PropertyType, Value.ToString(), out _))
                {
                    string[] memberNames = new string[] { "Invalid compare value" };
                    results.Add(new ValidationResult($"The value to compare [{Value}] provided for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}] is not valid for the property type [{propertyInfo.Name}]{System.Environment.NewLine}Valid entries are [{String.Join(",", Enum.GetNames(propertyInfo.PropertyType))}]", memberNames));
                }
            }

            // valid for true / false bool
            if (propertyInfo.PropertyType == typeof(bool))
            {
                // blank entry is permitted if using isTrue or isFalse otherwise check value
                if (Value != null)
                {
                    if(!bool.TryParse(Value.ToString(), out _))
                    { 
                        string[] memberNames = new string[] { "Invalid compare value" };
                        results.Add(new ValidationResult($"The value to compare [{Value}] provided for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}] is not valid for the property type [Boolean]{System.Environment.NewLine}Valid entries are [True, true, False, false, 1, 0]", memberNames));
                    }
                }
            }

            // valid for istrue / isfalse
            if (Value != null & IsOperatorTrueFalseTest())
            {
                if (!bool.TryParse(Value.ToString(), out _))
                {
                    string[] memberNames = new string[] { "Invalid compare value" };
                    results.Add(new ValidationResult($"The value to compare [{Value}] provided for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}] is not valid for the property type [Boolean]{System.Environment.NewLine}Valid entries are [True, true, False, false, 1, 0]", memberNames));
                }
            }

            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            return $"<div class=\"filter\" style=\"opacity: {((Enabled) ? "1" : "0.4")}\">{ToHTMLString()}</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }
        #endregion
    }

}
