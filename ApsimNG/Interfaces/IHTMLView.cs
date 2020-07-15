using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.EventArguments;

namespace UserInterface.Interfaces
{
    /// <summary>
    /// HTMLView interface.
    /// </summary>
    public interface IHTMLView
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
        void SetContents(string contents, bool allowModification, bool isURI = false);

        /// <summary>
        /// Return the edited markdown.
        /// </summary>
        /// <returns></returns>
        string GetMarkdown();

        /// <summary>
        /// Tells view to use a mono spaced font.
        /// </summary>
        void UseMonoSpacedFont();

        /// <summary>
        /// Called when the user wants to copy something in the HTMLView.
        /// </summary>
        /// <remarks>Ideally this would all be handled internally in the HTMLView.</remarks>
        event EventHandler<CopyEventArgs> Copy;
    }
}
