using System.Drawing;
using UserInterface.Intellisense;
using System.Collections.Generic;
using System;

namespace UserInterface.Interfaces
{

    /// <summary>
    /// Interface for a small intellisense window which displays the 
    /// completion options for a method.
    /// </summary>
    interface IMethodCompletionView : IDisposable
    {
        /// <summary>
        /// List of method completions for all overloads of this method.
        /// </summary>
        List<MethodCompletion> Completions { get; set; }

        /// <summary>
        /// Gets or sets the visibility of the window.
        /// </summary>
        bool Visible { get; set; }

        /// <summary>
        /// Gets or sets the location (top-left corner) of the popup window.
        /// </summary>
        Point Location { get; set; }
    }
}
