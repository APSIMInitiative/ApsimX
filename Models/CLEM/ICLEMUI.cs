using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM
{
    /// <summary>
    /// Interface for UI parameters associated with models.
    /// </summary>
    public interface ICLEMUI
    {
        /// <summary>
        /// Selected display tab
        /// </summary>
        string SelectedTab { get; set; }
    }
}
