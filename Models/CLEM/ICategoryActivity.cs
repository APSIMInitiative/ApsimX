using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM
{
    /// <summary>
    /// Activity that needs a category for transaction reporting
    /// </summary>
    public interface ICategoryActivity
    {
        /// <summary>
        /// Category for this activity
        /// </summary>
        string Category { get; set; }
    }
}
