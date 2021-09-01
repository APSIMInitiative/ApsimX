using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
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

        /// <inheritdoc/>
        public override Func<T, bool> Compile<T>()
        {
            // check that the T is attributabe
            if (!(typeof(IAttributable).IsAssignableFrom(typeof(T))))
                return (T t) => false;

            var filterParam = Expression.Parameter(typeof(T));
            var attProperty = Expression.Property(filterParam, typeof(IAttributable).GetProperty("Attributes"));
            
            var tag = Expression.Constant(AttributeTag);
            var compareValue = Expression.Constant(Value??"");

            MethodInfo method; // method to get the value
            Expression methodcall; // call the method with arguments
            Expression binary; // binary expression for lambda
            
            if (FilterStyle == AttributeFilterStyle.Exists)
            {
                // need to get the methodinfo from attributes (it is nested T.Attributes.Exists(tag))
                method = typeof(IndividualAttributeList).GetMethods().Where(m => m.Name == "Exists").FirstOrDefault();

                methodcall = Expression.Call(attProperty, method, tag);
                if (Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse)
                {
                    // Allow for IsTrue and IsFalse operator
                    var boolres = Expression.Constant(Operator == ExpressionType.IsTrue);
                    binary = Expression.MakeBinary(ExpressionType.Equal, methodcall, boolres);
                    return Expression.Lambda<Func<T, bool>>(binary, filterParam).Compile();
                }
                else
                {
                    binary = Expression.MakeBinary(Operator, methodcall, compareValue);
                    return Expression.Lambda<Func<T, bool>>(binary, filterParam).Compile();
                }
            }
            else
            {
                // need to get the methodinfo from attributes (it is nested T.Attributes.GetValue(tag))
                method = typeof(IndividualAttributeList).GetMethods().Where(m => m.Name == "GetValue").FirstOrDefault();

                // get compare value from Value - assumed to be single 
                // TODO: will also need string when haplotypes implemented
                compareValue = Expression.Constant(Convert.ToSingle(Value??"0.0"));
                methodcall = Expression.Call(attProperty, method, tag);

                var returnValue = Expression.Property(methodcall, "StoredValue");
                var returnValueAsFloat = Expression.Convert(returnValue, typeof(Single));
                binary = Expression.MakeBinary(Operator, returnValueAsFloat, compareValue);
                return Expression.Lambda<Func<T, bool>>(binary, filterParam).Compile();
            }
        }

        ///// <inheritdoc/>
        //public override Func<T, bool> Compile<T>()
        //{
        //    Func<T, bool> lambda = t =>
        //    {
        //        if (!(t is IAttributable attributable))
        //            return false;

        //        //if (!attributable.Attributes.Exists(AttributeTag))
        //        //    return false;

        //        // using the provided operator

        //        if (FilterStyle == AttributeFilterStyle.Exists)
        //        {
        //            bool boolResult = true;
        //            string value = Value?.ToString().ToLower();
        //            switch (Operator)
        //            {
        //                case ExpressionType.Equal:
        //                    boolResult = value == "true";
        //                    break;
        //                case ExpressionType.NotEqual:
        //                    boolResult = value != "true";
        //                    boolResult = false;
        //                    break;
        //                case ExpressionType.IsTrue:
        //                    boolResult = true;
        //                    break;
        //                case ExpressionType.IsFalse:
        //                    boolResult = false;
        //                    break;
        //                case ExpressionType.GreaterThan:
        //                case ExpressionType.GreaterThanOrEqual:
        //                case ExpressionType.LessThan:
        //                case ExpressionType.LessThanOrEqual:
        //                default:
        //                    throw new NotImplementedException($"The operator [{OperatorToSymbol()}] is not valid for the Atribute-based filter [f={Name}] of style [{FilterStyle}]");
        //            }
        //            return (attributable.Attributes.Exists(AttributeTag) == boolResult);
        //        }
        //        else
        //        {
        //            // using filter by value
        //            var ce1 = Convert.ToDecimal(Value ?? "");
        //            var ce2 = Convert.ToDecimal(attributable.Attributes.GetValue(AttributeTag)?.StoredValue ?? 0);

        //            switch (Operator)
        //            {
        //                case ExpressionType.Equal:
        //                    return ce2 == ce1;
        //                case ExpressionType.NotEqual:
        //                    return ce2 != ce1;
        //                case ExpressionType.GreaterThan:
        //                    return ce2 > ce1;
        //                case ExpressionType.GreaterThanOrEqual:
        //                    return ce2 >= ce1;
        //                case ExpressionType.LessThan:
        //                    return ce2 < ce1;
        //                case ExpressionType.LessThanOrEqual:
        //                    return ce2 <= ce1;
        //                case ExpressionType.IsTrue:
        //                case ExpressionType.IsFalse:
        //                default:
        //                    throw new NotImplementedException($"The operator [{OperatorToSymbol()}] is not valid for the Atribute-based filter [f={Name}] of style [{FilterStyle}]");
        //            }
        //        }
        //    };
        //    return lambda;
        //}

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
