namespace UserInterface.Commands
{
    using Interfaces;
    using Models.Core;
    using Models.Core.ApsimFile;
    using System;

    /// <summary>This command replaces a model with another model.</summary>
    public class ReplaceModelCommand : ICommand
    {
        /// <summary>The model being replaced</summary>
        private IModel originalModel;

        private Func<IModel, TreeViewNode> describeModel;

        /// <summary>Constructor.</summary>
        /// <param name="originalModel">The model being replaced.</param>
        /// <param name="replacement">The replacement model.</param>
        /// <param name="describeModel"></param>

        public ReplaceModelCommand(IModel originalModel, IModel replacement, Func<IModel, TreeViewNode> describeModel)
        {
            this.originalModel = originalModel;
            this.Replacement = replacement;
            this.describeModel = describeModel;
        }

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => originalModel;

        /// <summary>The replacement model.</summary>
        public IModel Replacement { get; set; }


        /// <summary>Perform the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            if (originalModel != null && Replacement != null)
            {
                Replacement = Structure.Replace(originalModel, Replacement);
                tree.RefreshNode(Replacement.FullPath, describeModel(Replacement));
            }
        }

        /// <summary>Undoes the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            if (originalModel != null && Replacement != null)
            {
                IModel newModel = Structure.Replace(Replacement, originalModel);
                modelChanged?.Invoke(newModel);
                tree.RefreshNode(newModel.FullPath, describeModel(originalModel));
            }
        }
    }
}
