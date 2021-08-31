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
using Display = Models.Core.DisplayAttribute;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Filter using property or method (withut arguments) of the IFilterable individual
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("An filter component using properties and methods of the individual")]
    [ValidParent(ParentType = typeof(IFilterGroup))]
    [Version(1, 0, 0, "")]
    public class FilterByProperty : Filter, IValidatableObject
    {
        /// <summary>
        /// The property or method to filter by
        /// </summary>
        [Description("Property or method")]
        [Required]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetParameters))]
        public string PropertyOfIndividual { get; set; }
        private IEnumerable<string> GetParameters() => Parent.Parameters.OrderBy(k => k);

        /// <summary>
        /// Constructor
        /// </summary>
        public FilterByProperty()
        {
            base.SetDefaults();
        }

        /// <inheritdoc/>
        public override Func<T, bool> Compile<T>()
        {
            // Check that the filter applies to objects of type T
            var info = Parent.GetProperty(PropertyOfIndividual);
            if (!info.DeclaringType.IsSubclassOf(typeof(T)))
                return (T t) => false;

            // Look for the property on T
            var genericType = Expression.Parameter(info.DeclaringType);
            var key = Expression.Property(genericType, info.Name);

            // Try convert the Value into the same data type as the property
            var type = info.PropertyType;
            var ce = type.IsEnum ? Enum.Parse(type, Value.ToString(), true) : Convert.ChangeType(Value??0, type);
            var value = Expression.Constant(ce);

            // Create a lambda that compares the filter value to the property on T

            // using the provided operator
            Expression binary; 
            if (Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse)
            {
                // Allow for IsTrue and IsFalse operator
                ce = (Operator == ExpressionType.IsTrue) ? true : false;
                binary = Expression.MakeBinary(ExpressionType.Equal, key, Expression.Constant(ce));
            }
            else
            {
                binary = Expression.MakeBinary(Operator, key, value);
            }
            
            var lambda = Expression.Lambda<Func<T, bool>>(binary, genericType).Compile();
            return lambda;
        }

        /// <summary>
        /// Convert sort to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return filterString(false);
        }

        /// <summary>
        /// Convert sort to html string
        /// </summary>
        /// <returns></returns>
        public string ToHTMLString()
        {
            return filterString(true);
        }

        private string filterString(bool htmltags)
        {
            using (StringWriter filterWriter = new StringWriter())
            {
                filterWriter.Write($"Filter:");
                bool truefalse = IsOperatorTrueFalseTest();
                if (truefalse)
                {
                    if (Operator == ExpressionType.IsFalse || Value?.ToString().ToLower() == "false")
                        filterWriter.Write(" not");
                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                }
                else
                {
                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(OperatorToSymbol(), "Unknown operator", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(Value?.ToString(), "No value", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                }
                return filterWriter.ToString();
            }
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if((Value is null || Value.ToString() == "") & !(Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse))
            {
                string[] memberNames = new string[] { "Missing filter compare value" };
                results.Add(new ValidationResult($"A value to compare with the Property is required for [f={Name}] in [f={Parent.Name}]", memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            return $"<div class=\"filter\" style=\"opacity: {((Enabled) ? "1" : "0.4")}\">{ToHTMLString()}</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            // allows for collapsed box and simple entry
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            // allows for collapsed box and simple entry
            return "";
        }
        #endregion
    }

}
