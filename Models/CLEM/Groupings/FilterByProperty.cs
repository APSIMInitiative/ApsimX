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
        public override Func<T, bool> Compile<T>()
        {
            // Check that the filter applies to objects of type T
            var info = Parent.GetProperty(Parameter);
            if (!info.DeclaringType.IsAssignableFrom(typeof(T)))
                return (T t) => false;

            // Look for the property on T
            var genericType = Expression.Parameter(info.DeclaringType);
            var key = Expression.Property(genericType, Parameter);

            // Try convert the Value into the same data type as the property
            var type = info.PropertyType;
            var ce = type.IsEnum ? Enum.Parse(type, Value.ToString(), true) : Convert.ChangeType(Value, type);
            var value = Expression.Constant(ce);

            // Create a lambda that compares the filter value to the property on T
            // using the provided operator
            var binary = Expression.MakeBinary(Operator, key, value);
            var lambda = Expression.Lambda<Func<T, bool>>(binary, genericType).Compile();
            return lambda;
        }
    }

}
