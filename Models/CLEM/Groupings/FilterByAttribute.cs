using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Display = Models.Core.DisplayAttribute;

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
    [ValidParent(ParentType = typeof(RuminantDestockGroup))]
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
                return false;
            };

            return lambda;
        }
    }

}
