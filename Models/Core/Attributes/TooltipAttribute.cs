using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Core
{
    /// <summary>Stores a tooltip for a property.</summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class TooltipAttribute : Attribute
    {
        /// <summary>
        /// Tooltip to be displayed in the UI.
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// Creates an instance of a tooltip attribute.
        /// </summary>
        /// <param name="tooltip">Tooltip to be displayed in the UI.</param>
        public TooltipAttribute(string tooltip)
        {
            this.Tooltip = tooltip;
        }
    }
}
