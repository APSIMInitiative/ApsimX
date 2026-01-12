using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Stores specified child components with descriptive summary details for handling display
    /// </summary>
    public class ChildComponentGroup
    {
        /// <summary>
        /// List of selected models
        /// </summary>
        public IEnumerable<IModel> SelectedModels { get; set; }

        /// <summary>
        /// Switch to determine if the group is included or ignored
        /// </summary>
        public bool Include { get; set; } = true;

        /// <summary>
        /// Text to display at start of group
        /// </summary>
        public string Introduction { get; set; }

        /// <summary>
        /// Text to display if group is empty
        /// </summary>
        public string Missing { get; set; }

        /// <summary>
        /// Name of border class to use being ignored if empty string
        /// </summary>
        public string BorderCssClass { get; set; }

        /// <summary>
        /// Label to identify this group
        /// </summary>
        public string Id { get; set; }
    }
}
