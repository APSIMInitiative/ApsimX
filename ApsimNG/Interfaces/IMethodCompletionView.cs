namespace UserInterface.Interfaces
{
    using System.Drawing;
    using Intellisense;
    using System.Collections.Generic;
    /// <summary>
    /// Interface for a small intellisense window which displays the 
    /// completion options for a method.
    /// </summary>
    interface IMethodCompletionView
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

        void Destroy();
    }
}
