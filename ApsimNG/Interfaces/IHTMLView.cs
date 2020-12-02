using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.EventArguments;

namespace UserInterface.Interfaces
{
    /// <summary>
    /// An interface for a HTML view.
    /// </summary>
    interface IHTMLView
    {
        /// <summary>
        /// Path to find images on.
        /// </summary>
        string ImagePath { get; set; }

        /// <summary>
        /// Set the contents of the control. Can be RTF, HTML or MarkDown. If 
        /// the contents are markdown and 'allowModification' = true then
        /// user will be able to edit markdown.
        /// </summary>
        void SetContents(string contents, bool allowModification, bool isURI);

        /// <summary>
        /// Tells view to use a mono spaced font.
        /// </summary>
        void UseMonoSpacedFont();
    }
}
