namespace UserInterface.Commands
{
    using System;
    using Interfaces;
    using Models.Core;
    using Models.Core.ApsimFile;

    /// <summary>This command moves a model from one Parent Node to another.</summary>
    class MoveModelCommand : ICommand
    {
        private IModel fromModel;
        private IModel toParent;
        private Func<IModel, TreeViewNode> describeModel;
        private IModel fromParent;
        private string originalName;
        private int originalPosition;

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => fromModel;

        /// <summary>Constructor.</summary>
        public MoveModelCommand(IModel model, IModel newParent, Func<IModel, TreeViewNode> describeModel)
        {
            if (model.ReadOnly)
                throw new ApsimXException(model, string.Format("Unable to move {0} to {1} - {0} is read-only.", model.Name, newParent.Name));
            if (newParent.ReadOnly)
                throw new ApsimXException(newParent, string.Format("Unable to move {0} to {1} - {1} is read-only.", model.Name, newParent.Name));
            if (describeModel == null)
                throw new ArgumentNullException(nameof(describeModel));
            fromModel = model;
            toParent = newParent;
            this.describeModel = describeModel;
        }

        /// <summary>Perform the command.</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            fromParent = fromModel.Parent;

            // The Move method may rename the FromModel. Go get the original name in case of
            // Undo later.
            originalName = fromModel.Name;
            originalPosition = tree.GetNodePosition(fromModel.FullPath);
            string originalPath = this.fromModel.FullPath;

            // Move model.
            Structure.Move(fromModel, toParent);
            tree.Delete(originalPath);
            tree.AddChild(toParent.FullPath, describeModel(fromModel));
            tree.SelectedNode = fromModel.FullPath;
        }

        /// <summary>Undo the command.</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            tree.Delete(fromModel.FullPath);
            Structure.Move(fromModel, fromParent);
            fromModel.Name = originalName;
            tree.AddChild(fromParent.FullPath, describeModel(fromModel), originalPosition);
            tree.SelectedNode = fromModel.FullPath;
        }
    }
}
