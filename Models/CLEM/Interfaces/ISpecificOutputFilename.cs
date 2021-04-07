using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface to provide specified filename for html output produced
    /// </summary>
    public interface ISpecificOutputFilename
    {
        /// <summary>
        /// Name of output filename
        /// </summary>
        string HtmlOutputFilename { get; }
    }
}
