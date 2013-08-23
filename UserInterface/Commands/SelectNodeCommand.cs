using UserInterface.Views;
using Model.Core;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command changes the 'CurrentNode' in the ExplorerView.
    /// </summary>
    class SelectNodeCommand : ICommand
    {
        private IExplorerView ExplorerView;
        private string SavedSelection;
        private string NewSelection;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SelectNodeCommand(IExplorerView ExplorerView, string NewSelection)
        {
            this.ExplorerView = ExplorerView;
            this.NewSelection = NewSelection;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public object Do()
        {
            SavedSelection = ExplorerView.CurrentNodePath;
            ExplorerView.CurrentNodePath = NewSelection;
            return null;
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public object Undo()
        {
            ExplorerView.CurrentNodePath = SavedSelection;
            return null;
        }

    }
}
