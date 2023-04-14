namespace UserInterface.Commands
{
    using Presenters;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Interfaces;
    using System;

    /// <summary>
    /// A command for renaming a model.
    /// </summary>
    class RenameModelCommand : ICommand
    {
        private Model modelToRename;
        private string newName;
        private string originalName;

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => modelToRename;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenameModelCommand"/> class.
        /// </summary>
        /// <param name="modelToRename">The model to rename.</param>
        /// <param name="newName">The new name.</param>
        public RenameModelCommand(Model modelToRename, string newName)
        {
            if (modelToRename.ReadOnly)
                throw new ApsimXException(modelToRename, string.Format("Unable to rename {0} - it is read-only.", modelToRename.Name));
            this.modelToRename = modelToRename;
            this.newName = newName.Trim();
        }

        /// <summary>Performs the command.</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            string originalPath = this.modelToRename.FullPath;

            // Get original value of property so that we can restore it in Undo if needed.
            originalName = this.modelToRename.Name;

            // Set the new name.
            Structure.Rename(modelToRename, newName);
            tree.Rename(originalPath, this.modelToRename.Name);
            modelChanged(modelToRename);
            //select root node before changing name, so that we can select the changed node afterwards
            tree.SelectedNode = ".Simulations";
            tree.SelectedNode = modelToRename.FullPath;
        }

        /// <summary>Undoes the command.</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            tree.Rename(modelToRename.FullPath, originalName);
            modelToRename.Name = originalName;
            modelChanged(modelToRename);
            //select root node before changing name, so that we can select the changed node afterwards
            tree.SelectedNode = ".Simulations";
            tree.SelectedNode = modelToRename.FullPath;
        }
    }
}
