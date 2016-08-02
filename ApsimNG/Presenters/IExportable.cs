using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Defines an interface for Views that are printable.
    /// </summary>
    interface IExportable
    {
        /// <summary>
        /// Convert the node to HTML and return the HTML string. Returns
        /// null if not convertable. The 'folder' is where any images
        /// should be created.
        /// </summary>
        string ConvertToHtml(string folder);
    }
}
