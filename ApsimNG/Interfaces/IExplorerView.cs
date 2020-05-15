namespace UserInterface.Interfaces
{
    /// <summary>
    /// The interface for an explorer view.
    /// NB: All node paths are compatible with XmlHelper node paths.
    /// e.g.  /simulations/test/clock
    /// </summary>
    public interface IExplorerView
    {
        /// <summary>The tree on the left side of the explorer view</summary>
        ITreeView Tree { get; }

        /// <summary>The toolstrip at the top of the explorer view</summary>
        IToolStripView ToolStrip { get; }

        /// <summary>
        /// Add a view to the right hand panel.
        /// </summary>
        void AddRightHandView(object control);

        /// <summary>
        /// Add a description to the right hand view.
        /// </summary>
        /// <param name="description">The description to show.</param>
        void AddDescriptionToRightHandView(string description);

        /// <summary>Get a screen shot of the right hand panel.</summary>
        System.Drawing.Image GetScreenshotOfRightHandPanel();
    }
}
