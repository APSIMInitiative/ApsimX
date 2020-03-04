namespace UserInterface.Commands
{
    using Models.Core;
    using Interfaces;

    /// <summary>This command deletes a model</summary>
    public class DeleteModelCommand : ICommand
    {
        /// <summary>The model to delete</summary>
        private IModel modelToDelete;

        /// <summary>The node description</summary>
        private TreeViewNode nodeDescription;

        /// <summary>The parent model.</summary>
        private IModel parent;

        /// <summary>The explorer view</summary>
        private IExplorerView explorerView;

        /// <summary>Indicates whether the model was deleted successfully</summary>
        private bool modelWasRemoved;

        /// <summary>The position of the model in the list of child models.</summary>
        private int pos;

        /// <summary>The constructor</summary>
        /// <param name="modelToDelete">The model to delete</param>
        /// <param name="explorerView">The explorer view.</param>
        public DeleteModelCommand(IModel modelToDelete, TreeViewNode nodeDescription, IExplorerView explorerView)
        {
            if (modelToDelete.ReadOnly)
                throw new ApsimXException(modelToDelete, string.Format("Unable to delete {0} - it is read-only.", modelToDelete.Name));
            this.modelToDelete = modelToDelete;
            this.nodeDescription = nodeDescription;
            this.explorerView = explorerView;
            this.parent = modelToDelete.Parent;
        }

        /// <summary>Perform the command</summary>
        /// <param name="commandHistory">The command history instance</param>
        public void Do(CommandHistory commandHistory)
        {
            this.explorerView.Tree.Delete(Apsim.FullPath(this.modelToDelete));
            pos = this.parent.Children.IndexOf(this.modelToDelete as Model);
            modelWasRemoved = Apsim.Delete(this.modelToDelete as Model);
        }

        /// <summary>Undo the command</summary>
        /// <param name="commandHistory">The command history instance</param>
        public void Undo(CommandHistory commandHistory)
        {
            if (this.modelWasRemoved)
            {
                this.parent.Children.Insert(pos, this.modelToDelete as Model);
                this.explorerView.Tree.AddChild(Apsim.FullPath(this.parent), nodeDescription, pos);
                Apsim.ClearCache(this.modelToDelete);
            }
        }
    }
}
