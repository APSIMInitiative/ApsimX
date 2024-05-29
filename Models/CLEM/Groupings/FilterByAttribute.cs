using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
    [HelpUri(@"Content/Features/Filters/FilterByAttribute.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
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
            List<ValidationResult> results = new List<ValidationResult>();
            ValidationContext context = new ValidationContext(this, null, null);
            if (Validator.TryValidateObject(this, context, results, true))
            {
                Initialise();
                BuildRule();
            }
        }

        /// <inheritdoc/>
        public override Func<T, bool> Compile<T>()
        {
            var simpleFilterParam = Expression.Parameter(typeof(T));
            var tag = Expression.Constant(AttributeTag, typeof(string));

            var iattParam = Expression.TypeAs(simpleFilterParam, typeof(IAttributable));
            var attProperty = Expression.Property(iattParam, "Attributes");
            var attIsNull = Expression.Equal(attProperty, Expression.Constant(null));

            var falseConstant = Expression.Constant(false, typeof(bool));

            var existsmethod = typeof(IndividualAttributeList).GetMethod("Exists");
            var exists = Expression.Call(attProperty, existsmethod, tag);

            Expression simpleBinary;
            ConditionalExpression block;
            if (FilterStyle == AttributeFilterStyle.Exists)
            {
                var simpleVal = Expression.Constant(Convert.ChangeType(Value ?? 0, typeof(bool)));
                if (Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse)
                {
                    // Allow for IsTrue and IsFalse operator
                    simpleVal = Expression.Constant(Operator == ExpressionType.IsTrue);
                    simpleBinary = Expression.MakeBinary(ExpressionType.Equal, exists, simpleVal);
                }
                else
                {
                    simpleBinary = Expression.MakeBinary(Operator, exists, simpleVal);
                }

                block = Expression.Condition(
                        attIsNull,
                        // false
                        falseConstant,
                        // true
                        Expression.Condition(
                            exists,
                            // true
                            simpleBinary,
                            // false
                            falseConstant
                            )
                    );
            }
            else
            {
                if (Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse)
                    throw new ApsimXException(this, $"Invalid FilterByAttribute operator [{OperatorToSymbol()}] in [f={NameWithParent}]");

                var valueMethod = typeof(IndividualAttributeList).GetMethod("GetValue");
                var valueVal = Expression.TypeAs(Expression.Call(attProperty, valueMethod, tag), typeof(IndividualAttribute));
                var valueStored = Expression.Property(valueVal, "Value");
                var valueStoredType = ((PropertyInfo)valueStored.Member).PropertyType;
                var value = Expression.Convert(valueStored, valueStoredType);

                var simpleVal = Expression.Constant(Convert.ChangeType(Value ?? 0, valueStoredType));
                simpleBinary = Expression.MakeBinary(Operator, value, simpleVal);

                block = Expression.Condition(
                    // Attributes exist
                        attIsNull,
                        // false
                        falseConstant,
                        // true
                        Expression.Condition(
                            exists,
                            // true
                            simpleBinary,
                            // false
                            falseConstant
                            )
                    );
            }

            return Expression.Lambda<Func<T, bool>>(block, simpleFilterParam).Compile();
        }

        /// <inheritdoc/>
        public override void Initialise()
        {
        }

        /// <inheritdoc/>
        public override void BuildRule()
        {
            if (Rule is null)
                Rule = Compile<IFilterable>();
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
            using StringWriter filterWriter = new();
            filterWriter.Write($"Filter:");
            bool truefalse = IsOperatorTrueFalseTest();
            if (FilterStyle == AttributeFilterStyle.Exists | truefalse)
            {
                bool nothingAdded = true;
                if (truefalse)
                    if (Operator == ExpressionType.IsFalse | Value?.ToString().ToLower() == "false")
                    {
                        filterWriter.Write(" does not have");
                        nothingAdded = false;
                    }
                if (nothingAdded)
                    filterWriter.Write(" has");

                filterWriter.Write($" attribute {CLEMModel.DisplaySummaryValueSnippet(AttributeTag, "No tag", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
            }
            else
            {
                filterWriter.Write($" Attribute {CLEMModel.DisplaySummaryValueSnippet(AttributeTag, "No tag", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
                filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(OperatorToSymbol(), "Unknown operator", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
                filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(Value?.ToString(), "No value", htmlTags: htmltags, entryStyle: HTMLSummaryStyle.Filter)}");
            }
            return filterWriter.ToString();
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            string[] memberNames;
            if (FilterStyle == AttributeFilterStyle.ByValue)
            {
                if ((Value is null || Value.ToString() == "") & !(Operator == ExpressionType.IsTrue | Operator == ExpressionType.IsFalse))
                {
                    memberNames = new string[] { "Missing filter compare value" };
                    yield return new ValidationResult($"A value to compare with the Attribute tag is required for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}]", memberNames);
                }
            }
            else
            {
                switch (Operator)
                {
                    case ExpressionType.Equal:
                    case ExpressionType.NotEqual:
                        if (!(Value is null || Value.ToString() == ""))
                            if (!bool.TryParse(Value.ToString(), out _))
                            {
                                memberNames = new string[] { "Invalid value" };
                                yield return new ValidationResult($"The value [{Value}] is not valid for the [{Operator}] operator selected for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}].{Environment.NewLine}Expecting True or False", memberNames);
                            }
                        break;
                    case ExpressionType.IsTrue:
                    case ExpressionType.IsFalse:
                        if (!(Value is null || Value.ToString() == ""))
                            if (!bool.TryParse(Value.ToString(), out _))
                            {
                                memberNames = new string[] { "Invalid value" };
                                yield return new ValidationResult($"The value [{Value}] is not valid for the [{Operator}] operator selected for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}].{Environment.NewLine}Expecting True or False, or blank entry", memberNames);
                            }
                        break;
                    default:
                        memberNames = new string[] { "Invalid operator" };
                        yield return new ValidationResult($"The [{Operator}] operator is not valid for attribute filtering using the [Exists] style for [f={Name}] in [f={(Parent as CLEMModel).NameWithParent}]{Environment.NewLine}Expecting [IsTrue], [IsFalse], [Equals] or [NotEquals]", memberNames);
                        break;
                }
            }
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
