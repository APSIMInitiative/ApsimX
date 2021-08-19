using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual filter rule based on Attribute exists or value
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantFeedGroupMonthly))]
    [ValidParent(ParentType = typeof(RuminantFeedGroup))]
    [ValidParent(ParentType = typeof(RuminantGroup))]
    [ValidParent(ParentType = typeof(AnimalPriceGroup))]
    [Version(1, 0, 0, "")]
    public class FilterByAttribute : Filter
    {
        /// <summary>
        /// Name of attribute to filter by
        /// </summary>
        [Description("Name of attribute to filter by")]
        [Required]
        public string Attribute { get; set; }

        /// <summary>
        /// Style to assess attribute
        /// </summary>
        [Description("Means of assessing attribute")]
        [Required]
        public AttributeFilterStyle FilterStyle { get; set; }

        /// <inheritdoc/>
        public override Func<T, bool> CompileRule<T>()
        {
            Func<T, bool> lambda = t =>
            {
                if (!(t is IAttributable attributable))
                    return false;

                if (!attributable.Attributes.Exists(Attribute))
                    return false;

                return attributable.Attributes.GetValue(Attribute).storedValue == Value;
            };

            return lambda;
        }
    }

}
