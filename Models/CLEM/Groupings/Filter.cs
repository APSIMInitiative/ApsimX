using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Display = Models.Core.DisplayAttribute;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual filter term for ruminant group of filters to identify individual ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFilterGroup))]
    [Version(1, 0, 0, "")]
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
        /// Name of parameter to filter by
        /// </summary>
        [Description("Operator to use for filtering")]
        [Required]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetOperators))]
        public ExpressionType Operator { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected object[] GetOperators() => new object[]
        {
            ExpressionType.Equal,
            ExpressionType.NotEqual,
            ExpressionType.LessThan,
            ExpressionType.LessThanOrEqual,
            ExpressionType.GreaterThan,
            ExpressionType.GreaterThanOrEqual
        };

        /// <summary>
        /// Value to check for filter
        /// </summary>
        [Description("Value to filter by")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Value to filter by required")]
        public object Value { get; set; }

        /// <summary>
        /// Takes the conditions set by the user and converts them to a logical test as a lambda expression
        /// </summary>
        //[EventSubscribe("StartOfSimulation")]
        public abstract Func<T, bool> CompileRule<T>();
    }
}
