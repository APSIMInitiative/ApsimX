using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UserInterface.Interfaces
{
    internal interface ICompletionItem
    {
        string Description { get; }
        string DisplayText { get; }

        /// <summary>
        /// Image/icon to be shown in intellisense.
        /// </summary>
        /// <remarks>Should this really be a Gdk.Pixbuf? System.Drawing.Image might make more sense.</remarks>
        Gdk.Pixbuf Image { get; }
        string Units { get; }
        string CompletionText { get; }

        /// <summary>
        /// Type of the completion item.
        /// </summary>
        /// <remarks>Should probably be a Type, not a string.</remarks>
        string ReturnType { get; }

        /// <summary>
        /// Is it a method
        /// </summary>
        /// <remarks>Probably better to subclass method completion items.</remarks>
        bool IsMethod { get; }
    }
}
