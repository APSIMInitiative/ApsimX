namespace UserInterface.Commands
{
    using Presenters;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Interfaces;
    using System;
    using APSIM.Core;

    /// <summary>
    /// A command for renaming a model.
    /// </summary>
    class RenameModelCommand : ICommand
    {
        private Node nodeToRename;
        private string newName;
        private string originalName;

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => nodeToRename.Model as IModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenameModelCommand"/> class.
        /// </summary>
        /// <param name="nodeToRename">The model to rename.</param>
        /// <param name="newName">The new name.</param>
        public RenameModelCommand(Node nodeToRename, string newName)
        {
            if ((nodeToRename.Model as IModel).ReadOnly)
                throw new Exception($"Unable to rename {nodeToRename.Name} - it is read-only.");
            this.nodeToRename = nodeToRename;
            this.newName = newName.Trim();
        }

        /// <summary>Performs the command.</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            string originalPath = this.nodeToRename.FullNameAndPath;

            // Get original value of property so that we can restore it in Undo if needed.
            originalName = this.nodeToRename.Name;

            // Set the new name.
            nodeToRename.Rename(newName);

            tree.Rename(originalPath, this.nodeToRename.Name);
            modelChanged(nodeToRename);
            //select root node before changing name, so that we can select the changed node afterwards
            tree.SelectedNode = ".Simulations";
            tree.SelectedNode = nodeToRename.FullNameAndPath;
        }

        /// <summary>Undoes the command.</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            tree.Rename(nodeToRename.FullNameAndPath, originalName);
            nodeToRename.Rename(originalName);
            modelChanged(nodeToRename);
            //select root node before changing name, so that we can select the changed node afterwards
            tree.SelectedNode = ".Simulations";
            tree.SelectedNode = nodeToRename.FullNameAndPath;
        }
    }
}
