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
        protected static object[] GetOperators() =>
        [
            ExpressionType.Equal,
            ExpressionType.NotEqual,
            ExpressionType.LessThan,
            ExpressionType.LessThanOrEqual,
            ExpressionType.GreaterThan,
            ExpressionType.GreaterThanOrEqual,
            ExpressionType.IsTrue,
            ExpressionType.IsFalse
        ];

        /// <summary>
        /// Convert the operator to a symbol
        /// </summary>
        /// <returns>Operator as symbol</returns>
        public string OperatorToSymbol()
        {
            return Operator switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.NotEqual => "!=",
                ExpressionType.IsTrue => "is",
                ExpressionType.IsFalse => "not",
                _ => Operator.ToString(),
            };
        }

        /// <summary>
        /// Is operator a true false test
        /// </summary>
        /// <returns>Operator as symbol</returns>
        public bool IsOperatorTrueFalseTest()
        {
            return Operator switch
            {
                ExpressionType.Equal or ExpressionType.NotEqual => (Value?.ToString().ToLower() == "true" | Value?.ToString().ToLower() == "false"),
                ExpressionType.IsTrue or ExpressionType.IsFalse => true,
                _ => false,
            };
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
