namespace UserInterface.Presenters
{
    using global::UserInterface.Commands;
    using Interfaces;
    using Models.Core;
    using Models.Core.ApsimFile;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Views;

    /// <summary>This presenter lets the user add a model.</summary>
    public class AddModelPresenter : IPresenter
    {
        /// <summary>The model to add a child model to.</summary>
        private IModel model;

        /// <summary>The Add model button</summary>
        private ButtonView addModelButton;

        /// <summary>The tree control.</summary>
        private TreeView tree;

        /// <summary>The filter edit control.</summary>
        private EditView filterEdit;

        /// <summary>The parent explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The allowable child models.</summary>
        private IEnumerable<Apsim.ModelDescription> allowableChildModels;

        /// <summary>Attach the specified Model and View.</summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            this.explorerPresenter = explorerPresenter;

            tree = (view as ViewBase).GetControl<TreeView>("treeview");
            addModelButton = (view as ViewBase).GetControl<ButtonView>("button");
            filterEdit = (view as ViewBase).GetControl<EditView>("filterEdit");

            allowableChildModels = Apsim.GetAllowableChildModels(this.model);

            tree.ReadOnly = true;

            PopulateTree(allowableChildModels);

            // Trap events from the view.
            addModelButton.Clicked += OnAddButtonClicked;
            tree.DragStarted += OnDragStart;
            tree.DoubleClicked += OnAddButtonClicked;
            filterEdit.Changed += OnFilterChanged;
        }

        /// <summary>Populate the tree control.</summary>
        /// <param name="models"></param>
        private void PopulateTree(IEnumerable<Apsim.ModelDescription> models)
        {
            var rootNode = new TreeViewNode()
            {
                Name = "Models",
                ResourceNameForImage = ExplorerPresenter.GetIconResourceName(typeof(Simulations), null, null)
            };

            foreach (var modelThatCanBeAdded in models)
                AddTreeNodeIfDoesntExist(modelThatCanBeAdded, rootNode);

            tree.Populate(rootNode);
            if (models.Count() < 10)
                tree.ExpandChildren(".Models");
        }

        private static void AddTreeNodeIfDoesntExist(Apsim.ModelDescription modelThatCanBeAdded, TreeViewNode parent)
        {
            var namespaceWords = modelThatCanBeAdded.ModelType.Namespace.Split(".".ToCharArray()).ToList();

            // Remove the first namespace word ('Models')
            namespaceWords.Remove(namespaceWords.First());

            foreach (var namespaceWord in namespaceWords.Where(word => word != "Models"))
            {
                var node = parent.Children.Find(n => n.Name == namespaceWord);
                if (node == null)
                {
                    node = new TreeViewNode()
                    {
                        Name = namespaceWord,
                        ResourceNameForImage = ExplorerPresenter.GetIconResourceName(typeof(Folder), null, null)
                    };
                    parent.Children.Add(node);
                }
                parent = node;
            }

            // Add the last model
            var description = new TreeViewNode()
            {
                Name = modelThatCanBeAdded.ModelName,
                ResourceNameForImage = ExplorerPresenter.GetIconResourceName(modelThatCanBeAdded.ModelType, modelThatCanBeAdded.ModelName, modelThatCanBeAdded.ResourceString)
            };
            parent.Children.Add(description);
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            addModelButton.Clicked -= this.OnAddButtonClicked;
            tree.DragStarted -= this.OnDragStart;
            tree.DoubleClicked -= OnAddButtonClicked;
            filterEdit.Changed -= OnFilterChanged;
        }

        private Apsim.ModelDescription GetModelDescription(string namePath)
        {
            Type selectedType = typeof(IModel).Assembly.GetType(tree.SelectedNode);
            if (selectedType != null)
                return allowableChildModels.FirstOrDefault(m => m.ModelType == selectedType);

            // Try a resource model (e.g. wheat).
            string modelName = tree.SelectedNode.Split('.').Last();
            Apsim.ModelDescription[] resourceModels = allowableChildModels.Where(c => !string.IsNullOrEmpty(c.ResourceString)).ToArray();
            return resourceModels.FirstOrDefault(m => m.ModelName == modelName);
        }

        /// <summary>The user has clicked the add button.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnAddButtonClicked(object sender, EventArgs e)
        {
            try
            {
                Apsim.ModelDescription selectedModelType = GetModelDescription(tree.SelectedNode);

                if (selectedModelType != null)
                {
                    this.explorerPresenter.MainPresenter.ShowWaitCursor(true);
                    IModel child = (IModel)Activator.CreateInstance(selectedModelType.ModelType, true);
                    child.Name = selectedModelType.ModelName;
                    if (child is ModelCollectionFromResource resource)
                        resource.ResourceName = selectedModelType.ModelName;

                    var command = new AddModelCommand(Apsim.FullPath(this.model), child, explorerPresenter);
                    explorerPresenter.CommandHistory.Add(command, true);
                }
            }
            finally
            {
                this.explorerPresenter.MainPresenter.ShowWaitCursor(false);
            }
        }

        /// <summary>A node has begun to be dragged.</summary>
        /// <param name="sender">Sending object</param>
        /// <param name="e">Drag arguments</param>
        private void OnDragStart(object sender, DragStartArgs e)
        {
            e.DragObject = null; // Assume failure
            string modelName = e.NodePath;

            // We want to create an object of the named type
            Type modelType = null;
            this.explorerPresenter.MainPresenter.ShowWaitCursor(true);
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type t in assembly.GetTypes())
                    {
                        if (t.FullName == modelName.TrimStart('.') && t.IsPublic && t.IsClass)
                        {
                            modelType = t;
                            break;
                        }
                    }
                }

                if (modelType != null)
                {
                    object child = Activator.CreateInstance(modelType, true);
                    string childString = FileFormat.WriteToString(child as IModel);
                    explorerPresenter.SetClipboardText(childString);

                    DragObject dragObject = new DragObject();
                    dragObject.NodePath = e.NodePath;
                    dragObject.ModelType = modelType;
                    dragObject.ModelString = childString;
                    e.DragObject = dragObject;
                }
            }
            finally
            {
                explorerPresenter.MainPresenter.ShowWaitCursor(false);
            }
        }

        /// <summary>
        /// The filter/search textbox has been modified by the user.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnFilterChanged(object sender, EventArgs e)
        {
            string filter = filterEdit.Value;
            PopulateTree(allowableChildModels
                            .Where(m => m.ModelName.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0));
        }
    }
}