namespace UserInterface.Commands
{
    using Presenters;
    using Interfaces;
    using Models.Core;
    using Models.Core.ApsimFile;
    using System;

    /// <summary>This command adds a model as a child of another model.</summary>
    public class AddModelCommand : ICommand
    {
        /// <summary>The parent model to add the model to.</summary>
        private IModel parent;

        /// <summary>A string representation of the child model to add.</summary>
        private IModel child;
        private Func<IModel, TreeViewNode> describeModel;

        /// <summary>A string representation of the child model to add.</summary>
        private string xmlOrJson;

        /// <summary>The model we're to add.</summary>
        private IModel modelToAdd;

        /// <summary>True if model was added.</summary>
        private bool modelAdded;

        /// <summary>Constructor.</summary>
        /// <param name="describeModel"></param>
        /// <param name="parent">The path of the parent model to add the child to.</param>
        /// <param name="child">The model to add.</param>
        public AddModelCommand(IModel parent, IModel child, Func<IModel, TreeViewNode> describeModel)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent), "Cannot add a child to a null parent");
            if (child == null)
                throw new ArgumentNullException(nameof(child), "Cannot add a null child");
            if (describeModel == null)
                throw new ArgumentNullException(nameof(describeModel));

            this.parent = parent;
            this.child = child;
            this.describeModel = describeModel;
        }

        /// <summary>Constructor - allows for adding a serialized model.</summary>
        /// <param name="parent">The model to which the child will be added.</param>
        /// <param name="textToAdd">The text string (xml/json) representation of the model to add.</param>
        /// <param name="describeModel"></param>
        public AddModelCommand(IModel parent, string textToAdd, Func<IModel, TreeViewNode> describeModel)
        {
            if (parent == null)
                throw new ArgumentNullException("Cannot add a child to a null parent");
            if (describeModel == null)
                throw new ArgumentNullException(nameof(describeModel));

            this.parent = parent;
            this.describeModel = describeModel;
            xmlOrJson = textToAdd;
        }

        /// <summary>
        /// The model which was changed by the command. This will be selected
        /// in the user interface when the command is undone/redone.
        /// </summary>
        public IModel AffectedModel => modelToAdd;

        /// <summary>Perform the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Do(ITreeView tree, Action<object> modelChanged)
        {
            if (xmlOrJson != null)
                modelToAdd = Structure.Add(xmlOrJson, parent);
            else
                modelToAdd = Structure.Add(child, parent);
            modelAdded = true;
            tree.AddChild(parent.FullPath, describeModel(modelToAdd));
            tree.SelectedNode = modelToAdd.FullPath;
        }

        /// <summary>Undoes the command</summary>
        /// <param name="tree">A tree view to which the changes will be applied.</param>
        /// <param name="modelChanged">Action to be performed if/when a model is changed.</param>
        public void Undo(ITreeView tree, Action<object> modelChanged)
        {
            if (modelAdded && modelToAdd != null)
            {
                string path = modelToAdd.FullPath;
                Structure.Delete(modelToAdd);
                tree.Delete(modelToAdd.FullPath);
            }
        }
    }
}
