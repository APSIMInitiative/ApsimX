namespace UserInterface.Commands
{
    using Interfaces;

    /// <summary>This command changes the 'CurrentNode' in the ExplorerView.</summary>
    class SelectNodeCommand : ICommand
    {
        /// <summary>The old path</summary>
        private string oldPath;
        /// <summary>The new path</summary>
        private string newPath;
        /// <summary>The explorer view</summary>
        private IExplorerView explorerView;

        /// <summary>Constructor.</summary>
        /// <param name="oldPath">The old path.</param>
        /// <param name="newPath">The new path.</param>
        /// <param name="explorerView">The explorer view.</param>
        public SelectNodeCommand(string oldPath, string newPath, IExplorerView explorerView)
        {
            this.explorerView = explorerView;
            this.oldPath = oldPath;
            this.newPath = newPath;
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Do(CommandHistory commandHistory)
        {
            explorerView.Tree.SelectedNode = newPath;
        }

        /// <summary>Undo the command</summary>
        /// <param name="commandHistory">The command history.</param>
        public void Undo(CommandHistory commandHistory)
        {
            // OldNodePath can be null on the very first time the GUI is opened. We
            // don't want to select a null node.
            if (oldPath != null)
                explorerView.Tree.SelectedNode = oldPath;
        }

    }
}
