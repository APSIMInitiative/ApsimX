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
    [ValidParent(ParentType = typeof(RuminantFeedGroupMonthly))]
    [ValidParent(ParentType = typeof(RuminantFeedGroup))]
    [ValidParent(ParentType = typeof(RuminantGroup))]
    [ValidParent(ParentType = typeof(AnimalPriceGroup))]
    [Version(1, 0, 0, "")]
    public class FilterByAttribute : Filter, IValidatableObject
    {
        [NonSerialized]
        private PropertyInfo attributesProperty;
        private ConstantExpression attributeTag;
        private ConstantExpression compareValueAttribute;
        private ConstantExpression compareValueValue;
        private ConstantExpression attributeExistsOperator;
        [NonSerialized]
        private MethodInfo methodExists;
        [NonSerialized]
        private MethodInfo methodValue;

        /// <summary>
        /// Attribute tag to filter by
        /// </summary>
        [Description("Attribute tag")]
        [Required]
        public string AttributeTag { get; set; }

        /// <summary>
        /// Style to assess attribute
        /// </summary>
        [Description("Assessment style")]
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
            // check that the T is attributabe
            if (!(typeof(IAttributable).IsAssignableFrom(typeof(T))))
                return (T t) => false;

            var filterParam = Expression.Parameter(typeof(T));
            var attProperty = Expression.Property(filterParam, attributesProperty);

            Expression methodcall; // call the method with arguments
            Expression binary; // binary expression for lambda
            
            if (FilterStyle == AttributeFilterStyle.Exists)
            {
                methodcall = Expression.Call(attProperty, methodExists, attributeTag);
                if (Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse)
                {
                    // Allow for IsTrue and IsFalse operator
                    binary = Expression.MakeBinary(ExpressionType.Equal, methodcall, attributeExistsOperator);
                    return Expression.Lambda<Func<T, bool>>(binary, filterParam).Compile();
                }
                else
                {
                    binary = Expression.MakeBinary(Operator, methodcall, compareValueAttribute);
                    return Expression.Lambda<Func<T, bool>>(binary, filterParam).Compile();
                }
            }
            else
            {
                // get compare value from Value - assumed to be single 
                // TODO: will also need string when haplotypes implemented
                methodcall = Expression.Call(attProperty, methodValue, attributeTag);

                var returnValue = Expression.Property(methodcall, "StoredValue");
                var returnValueAsFloat = Expression.Convert(returnValue, typeof(Single));
                binary = Expression.MakeBinary(Operator, returnValueAsFloat, compareValueValue);
                return Expression.Lambda<Func<T, bool>>(binary, filterParam).Compile();
            }
        }

        /// <summary>
        /// Initialise this filter by property 
        /// </summary>
        public override void Initialise()
        {
            attributesProperty = typeof(IAttributable).GetProperty("Attributes");
            attributeTag = Expression.Constant(AttributeTag);
            attributeExistsOperator = Expression.Constant(Operator == ExpressionType.IsTrue);
            compareValueAttribute = Expression.Constant(Value ?? "");
            compareValueValue = Expression.Constant(Convert.ToSingle(Value ?? "0.0"));
            // need to get the methodinfo from attributes (it is nested T.Attributes.Exists(tag))
            methodExists = typeof(IndividualAttributeList).GetMethods().Where(m => m.Name == "Exists").FirstOrDefault();
            // need to get the methodinfo from attributes (it is nested T.Attributes.GetValue(tag))
            methodValue = typeof(IndividualAttributeList).GetMethods().Where(m => m.Name == "GetValue").FirstOrDefault();
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
            return filterString(false);
        }

        /// <summary>
        /// Convert filter to html string
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
