namespace UserInterface.Commands
{
    using Models.Core;
    using Interfaces;
    using Models.Core.ApsimFile;
    using Presenters;
    using System;

    /// <summary>This command deletes a model</summary>
    public class DeleteModelCommand : ICommand
    {
        /// <summary>The model to delete</summary>
        private IModel modelToDelete;

        /// <summary>The node description</summary>
        private TreeViewNode nodeDescription;

        /// <summary>The parent model.</summary>
        private IModel parent;

        /// <summary>Indicates whether the model was deleted successfully</summary>
        private bool modelWasRemoved;

        /// <summary>The position of the model in the list of child models.</summary>
        public int Pos { get; private set; }

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => modelToDelete;

        /// <summary>The constructor</summary>
        /// <param name="modelToDelete">The model to delete</param>
        /// <param name="nodeDescription">The node description. This is used for the undo operation when we need to re-add the model.</param>
        /// <param name="explorerView">The explorer view.</param>
        public DeleteModelCommand(IModel modelToDelete, TreeViewNode nodeDescription)
        {
            if (modelToDelete.ReadOnly)
                throw new ApsimXException(modelToDelete, string.Format("Unable to delete {0} - it is read-only.", modelToDelete.Name));
            this.modelToDelete = modelToDelete;
            this.nodeDescription = nodeDescription;
            this.parent = modelToDelete.Parent;
        }

        /// <summary>Perform the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            Pos = this.parent.Children.IndexOf(this.modelToDelete as Model);
            string pathOfChildToDelete = modelToDelete.FullPath;
            modelWasRemoved = Structure.Delete(this.modelToDelete as Model);
            tree.Delete(pathOfChildToDelete);
        }

        /// <summary>Undo the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            if (modelWasRemoved)
            {
                parent.Children.Insert(Pos, this.modelToDelete as Model);
                Apsim.ClearCaches(this.modelToDelete);
                tree.AddChild(this.parent.FullPath, nodeDescription, Pos);
                tree.SelectedNode = modelToDelete.FullPath;
            }
        }
    }
}
