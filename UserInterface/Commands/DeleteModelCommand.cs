using UserInterface.Views;
using Model.Core;
using System.Xml;
using System;

namespace UserInterface.Commands
{
    /// <summary>
    /// This command changes the 'CurrentNode' in the ExplorerView.
    /// </summary>
    class DeleteModelCommand : ICommand
    {
        private IZone ParentZone;
        private string ParentPath;
        private object Model;
        private bool ModelRemoved;

        /// <summary>
        /// Constructor.
        /// </summary>
        public DeleteModelCommand(IZone ParentZone, string ParentPath, object Model)
        {
            this.ParentZone = ParentZone;
            this.ParentPath = ParentPath;
            this.Model = Model;
        }

        /// <summary>
        /// Perform the command
        /// </summary>
        public void Do(CommandHistory CommandHistory)
        {
            ModelRemoved = ParentZone.Models.Remove(Model);
            CommandHistory.InvokeModelStructureChanged(ParentPath);
        }

        /// <summary>
        /// Undo the command
        /// </summary>
        public void Undo(CommandHistory CommandHistory)
        {
            if (ModelRemoved)
            {
                ParentZone.AddModel(Model);
                CommandHistory.InvokeModelStructureChanged(ParentPath);
            }
        }

    }
}
