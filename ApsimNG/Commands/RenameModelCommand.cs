// -----------------------------------------------------------------------
// <copyright file="RenameModelCommand.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Commands
{
    using Models.Core;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A command for renaming a model.
    /// </summary>
    class RenameModelCommand : ICommand
    {
        private Model modelToRename;
        private string newName;
        Interfaces.IExplorerView explorerView;
        private string originalName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenameModelCommand"/> class.
        /// </summary>
        /// <param name="modelToRename">The model to rename.</param>
        /// <param name="newName">The new name.</param>
        /// <param name="explorerView">The explorer view.</param>
        public RenameModelCommand(Model modelToRename, string newName, Interfaces.IExplorerView explorerView)
        {
            this.modelToRename = modelToRename;
            this.newName = newName;
            this.explorerView = explorerView;
        }

        /// <summary>Performs the command.</summary>
        /// <param name="CommandHistory">The command history.</param>
        public void Do(CommandHistory CommandHistory)
        {
            string originalPath = Apsim.FullPath(modelToRename);

            // Get original value of property so that we can restore it in Undo if needed.
            originalName = modelToRename.Name;

            // Set the new name.
            this.modelToRename.Name = newName;
            Apsim.EnsureNameIsUnique(this.modelToRename);
            explorerView.Rename(originalPath, this.modelToRename.Name);
        }

        /// <summary>Undoes the command.</summary>
        /// <param name="CommandHistory">The command history.</param>
        public void Undo(CommandHistory CommandHistory)
        {
            explorerView.Rename(Apsim.FullPath(modelToRename), originalName);
            modelToRename.Name = originalName;
        }
    }
}
