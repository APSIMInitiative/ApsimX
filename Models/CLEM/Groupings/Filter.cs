using Models.CLEM.Interfaces;
using Models.Core;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Display = Models.Core.DisplayAttribute;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// abstract base filter not used on its own
    ///</summary> 
    [Serializable]
    public abstract class Filter : CLEMModel
    {
        /// <inheritdoc/>
        [JsonIgnore]
        public new IFilterGroup Parent
        {
            get => base.Parent as IFilterGroup;
            set => base.Parent = value;
        }

        /// <summary>
        /// Constructor to apply defaults
        /// </summary>
        public Filter()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.Filter;
        }


        /// <summary>
        /// The filter rule
        /// </summary>
        public Func<IFilterable, bool> Rule { get; protected set; }

        /// <summary>
        /// Clear any rules created
        /// </summary>
        public void ClearRule() { Rule = null; }

        /// <summary>
        /// Filter operator
        /// </summary>
        [Description("Operator")]
        [Required]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetOperators), Order = 2)]
        [System.ComponentModel.DefaultValueAttribute(ExpressionType.Equal)]
        public ExpressionType Operator { get; set; }
        
        /// <summary>
        /// Method to return available operators
        /// </summary>
        /// <returns></returns>
        protected object[] GetOperators() => new object[]
        {
            ExpressionType.Equal,
            ExpressionType.NotEqual,
            ExpressionType.LessThan,
            ExpressionType.LessThanOrEqual,
            ExpressionType.GreaterThan,
            ExpressionType.GreaterThanOrEqual,
            ExpressionType.IsTrue,
            ExpressionType.IsFalse
        };

        /// <summary>
        /// Convert the operator to a symbol
        /// </summary>
        /// <returns>Operator as symbol</returns>
        protected string OperatorToSymbol()
        {
            switch (Operator)
            {
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.IsTrue:
                    return "is";
                case ExpressionType.IsFalse:
                    return "not";
                default:
                    return Operator.ToString();
            }
        }

        /// <summary>
        /// Is operator a true false test
        /// </summary>
        /// <returns>Operator as symbol</returns>
        protected bool IsOperatorTrueFalseTest()
        {
            switch (Operator)
            {
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    return (Value?.ToString().ToLower() == "true" | Value?.ToString().ToLower() == "false");
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Value to check for filter
        /// </summary>
        [Description("Value to compare")]
        [Display(Order = 3)]
        public object Value { get; set; }

        /// <summary>
        /// Modified value to use 
        /// </summary>
        public virtual object ModifiedValueToUse
        {
            get { return Value; }
        }

        /// <summary>
        /// Takes the conditions set by the user and converts them to a logical test as a lambda expression
        /// </summary>
        public abstract Func<T, bool> Compile<T>() where T:IFilterable;

        /// <summary>
        /// A method to initialise this filter
        /// </summary>
        public abstract void Initialise();

        /// <summary>
        /// A method to build rules for this filter
        /// </summary>
        public abstract void BuildRule();
    }
}
