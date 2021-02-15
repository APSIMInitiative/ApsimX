
namespace UserInterface.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using EventArguments;

    /// <summary>
    /// The interface for an explorer view.
    /// NB: All node paths are compatible with XmlHelper node paths.
    /// e.g.  /simulations/test/clock
    /// </summary>
    public interface IPropertyTreeView
    {
        /// <summary>
        /// This event will be invoked when a node is selected not by the user
        /// but by an Undo command.
        /// </summary>
        event EventHandler<NodeSelectedArgs> SelectedNodeChanged;

        /// <summary>Refreshes the entire tree from the specified descriptions.</summary>
        /// <param name="nodeDescriptions">The nodes descriptions.</param>
        void Refresh(TreeViewNode nodeDescriptions);

        /// <summary>Gets or sets the currently selected node.</summary>
        /// <value>The selected node.</value>
        string SelectedNode { get; set; }

        /// <summary>
        /// Add a view to the right hand panel.
        /// </summary>
        void AddRightHandView(object control);

        /// <summary>Get a screen shot of the right hand panel.</summary>
        System.Drawing.Image GetScreenshotOfRightHandPanel();

        /// <summary>
        /// Get whatever text is currently on the clipboard
        /// </summary>
        /// <returns></returns>
        string GetClipboardText(string clipboardName);

        /// <summary>
        /// Place text on the clipboard
        /// </summary>
        /// <param name="text"></param>
        /// <param name="clipboardName">Name of the clipboard.</param>
        void SetClipboardText(string text, string clipboardName);

        /// <summary>
        /// Gets or sets the width of the tree view.
        /// </summary>
        Int32 TreeWidth { get; set; }

        /// <summary>Show the wait cursor</summary>
        /// <param name="wait">If true will show the wait cursor otherwise the normal cursor.</param>
        void ShowWaitCursor(bool wait);
    }

}
