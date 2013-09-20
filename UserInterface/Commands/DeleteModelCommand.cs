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
        private ModelCollection Parent;
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
            Parent = Model.Parent as ModelCollection;
            ModelRemoved = Parent.RemoveModel(Model);
            CommandHistory.InvokeModelStructureChanged(Parent.FullPath);
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
            if (ModelRemoved)
            {
                Parent.AddModel(Model);
                CommandHistory.InvokeModelStructureChanged(Parent.FullPath);
            }
        }

    }
}
