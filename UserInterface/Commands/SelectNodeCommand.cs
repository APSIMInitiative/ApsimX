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
        private string OldSelection;
        private string NewSelection;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SelectNodeCommand(IExplorerView ExplorerView, string OldSelection, string NewSelection)
        {
            this.ExplorerView = ExplorerView;
            this.OldSelection = OldSelection;
            this.NewSelection = NewSelection;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public object Do()
        {
            ExplorerView.CurrentNodePath = NewSelection;
            return null;
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public object Undo()
        {
            ExplorerView.CurrentNodePath = OldSelection;
            return null;
        }

    }
}
