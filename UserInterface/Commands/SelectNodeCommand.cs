using UserInterface.Views;
using Models.Core;
using UserInterface.Interfaces;

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
        public void Do(CommandHistory CommandHistory)
        {
            ExplorerView.CurrentNodePath = NewSelection;
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
            ExplorerView.CurrentNodePath = OldSelection;
        }

    }
}
