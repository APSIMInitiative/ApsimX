using UserInterface.Views;
using Models.Core;
using System.Xml;
using System;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command changes the 'CurrentNode' in the ExplorerView.
    /// </summary>
    class DeleteModelCommand : ICommand
    {
        private Model Model;
        private bool ModelRemoved;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeleteModelCommand(Model Model)
        {
            this.Model = Model;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            ModelRemoved = Model.Parent.Children.Remove(Model);
            CommandHistory.InvokeModelStructureChanged(Model.Parent.FullPath);
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
            if (ModelRemoved)
            {
                Model.Parent.Children.Add(Model);
                CommandHistory.InvokeModelStructureChanged(Model.Parent.FullPath);
            }
        }

    }
}
