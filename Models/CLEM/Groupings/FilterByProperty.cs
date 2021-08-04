using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    public class FilterByProperty : Filter
    {
        /// <summary>
        /// The item to filter by
        /// </summary>
        [Description("Property to filter by")]
        [Required]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetParameters))]
        public string Parameter { get; set; }
        private IEnumerable<string> GetParameters() => Parent.Parameters.OrderBy(k => k);

        /// <inheritdoc/>
        public override Func<T, bool> CompileRule<T>()
        {
            // Look for the property on type T
            var genericType = Expression.Parameter(typeof(T));
            var key = Expression.Property(genericType, Parameter);

            // Find the value we want to compare the property against
            var type = Parent.GetProperty(Parameter).PropertyType;
            object ce = type.IsEnum 
                ? Enum.Parse(type, Value.ToString(), true)
                : Convert.ChangeType(Value, type);
            var value = Expression.Constant(ce);

            // Convert the expression into a lambda
            var binary = Expression.MakeBinary(Operator, key, value);
            var lambda = Expression.Lambda<Func<T, bool>>(binary, genericType).Compile();
            return lambda;
        }
    }

}
