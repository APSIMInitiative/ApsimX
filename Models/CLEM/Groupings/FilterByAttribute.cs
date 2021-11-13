using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
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

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual filter rule based on Attribute exists or associated value
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Defines a filter rule using Attribute details of the individual")]
    [ValidParent(ParentType = typeof(IFilterGroup))]
    [Version(1, 0, 0, "")]
    public class FilterByAttribute : Filter, IValidatableObject
    {
        /// <summary>
        /// Attribute tag to filter by
        /// </summary>
        [Description("Attribute tag")]
        [Models.Core.Display(Order = 1)]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Attribute tag must be provided")]
        public string AttributeTag { get; set; }

        /// <summary>
        /// Style to assess attribute
        /// </summary>
        [Description("Assessment style")]
        [Models.Core.Display(Order = 4)]
        [Required]
        public AttributeFilterStyle FilterStyle { get; set; }

        ///<inheritdoc/>
        [EventSubscribe("Commencing")]
        protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            Initialise();
        }

        /// <inheritdoc/>
        public override Func<T, bool> Compile<T>()
        {
            Func<T, bool> simple = t => {
                var simpleFilterParam = Expression.Parameter(typeof(T));

                bool exists = (t as IAttributable).Attributes.Exists(AttributeTag);
                BinaryExpression simpleBinary;
                if (FilterStyle == AttributeFilterStyle.Exists)
                {
                    var simpleVal = Expression.Constant(Convert.ChangeType(Value ?? 0, typeof(bool)));
                    if (Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse)
                    {
                        // Allow for IsTrue and IsFalse operator
                        simpleVal = Expression.Constant((Operator == ExpressionType.IsTrue));
                        simpleBinary = Expression.MakeBinary(ExpressionType.Equal, Expression.Constant(exists), simpleVal);
                    }
                    else
                        simpleBinary = Expression.MakeBinary(Operator, Expression.Constant(exists), simpleVal);
                }
                else
                {
                    if (!exists) return false;
                    object attributeValue = (t as IAttributable).Attributes.GetValue(AttributeTag).StoredValue;
                    var simpleVal = Expression.Constant(Convert.ChangeType(Value ?? 0, attributeValue.GetType()));
                    var expAttributeVal = Expression.Constant(Convert.ChangeType(attributeValue, attributeValue.GetType()));

                    if (Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse)
                    {
                        throw new ApsimXException(this, $"Invalid FilterByAttribute operator [{OperatorToSymbol()}] in [f={this.NameWithParent}]");
                    }
                    else
                        simpleBinary = Expression.MakeBinary(Operator, expAttributeVal, simpleVal);
                }
                var simpleLambda = Expression.Lambda<Func<T, bool>>(simpleBinary, simpleFilterParam).Compile();
                return simpleLambda(t);
            };
            return simple;
        }

        /// <summary>
        /// Initialise this filter by property 
        /// </summary>
        public override void Initialise()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public FilterByAttribute()
        {
            base.SetDefaults();
        }

        /// <summary>
        /// Convert sort to string
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
            using (StringWriter filterWriter = new StringWriter())
            {
                filterWriter.Write($"Filter:");
                bool truefalse = IsOperatorTrueFalseTest();
                if (FilterStyle == AttributeFilterStyle.Exists | truefalse)
                {
                    filterWriter.Write(" is");
                    if (truefalse)
                        if (Operator == ExpressionType.IsFalse | Value?.ToString().ToLower() == "false")
                            filterWriter.Write(" not");
                    filterWriter.Write($" Attribute({CLEMModel.DisplaySummaryValueSnippet(AttributeTag, "No tag", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)})");
                }
                else
                {
                    filterWriter.Write($" Attribute-{CLEMModel.DisplaySummaryValueSnippet(AttributeTag, "No tag", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(OperatorToSymbol(), "Unknown operator", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(Value?.ToString(), "No value", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
                }
                return filterWriter.ToString();
            }
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if(FilterStyle == AttributeFilterStyle.ByValue)
                if ((Value is null || Value.ToString() == "") & !(Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse))
                {
                    string[] memberNames = new string[] { "Missing filter compare value" };
                    results.Add(new ValidationResult($"A value to compare with the Attribute value is required for [f={Name}] in [f={Parent.Name}]", memberNames));
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

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }
        #endregion

    }
}
