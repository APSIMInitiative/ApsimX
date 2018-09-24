namespace UserInterface.Interfaces
{
    using System.Drawing;

    /// <summary>
    /// Interface for a small intellisense window which displays the 
    /// completion options for a method.
    /// </summary>
    interface IMethodCompletionView
    {
        /// <summary>
        /// Gets or sets the method signature.
        /// </summary>
        string MethodSignature { get; set; }

        /// <summary>
        /// Gets or sets the method summary.
        /// </summary>
        string MethodSummary { get; set; }

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
