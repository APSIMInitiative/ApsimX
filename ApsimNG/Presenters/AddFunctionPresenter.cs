namespace UserInterface.Presenters
{
    using Interfaces;
    using Models.Core;
    using Models.Core.ApsimFile;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Views;

    /// <summary>This presenter lets the user add a model.</summary>
    public class AddFunctionPresenter : IPresenter
    {
        /// <summary>The model.</summary>
        private IModel model;

        /// <summary>The view.</summary>
        private IListButtonView view;

        /// <summary>The parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The allowable child models.</summary>
        private List<Type> allowableChildFunctions;

        /// <summary>Attach the specified Model and View.</summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            this.view = view as IListButtonView;
            this.explorerPresenter = explorerPresenter;

            this.allowableChildFunctions = Apsim.GetAllowableChildFunctions(this.model);

            this.view.List.IsModelList = true;
            this.view.List.Values = this.allowableChildFunctions.Select(m => m.FullName).ToArray();
            this.view.AddButton("Add", null, this.OnAddButtonClicked);

            // Trap events from the view.
            this.view.List.DoubleClicked += this.OnAddButtonClicked;
            this.view.List.DragStarted += this.OnDragStart;
            this.view.FilterChanged += this.OnFilterChanged;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            // Trap events from the view.
            this.view.List.DoubleClicked -= this.OnAddButtonClicked;
            this.view.List.DragStarted -= this.OnDragStart;
            this.view.FilterChanged -= this.OnFilterChanged;
        }

        /// <summary>The user has clicked the add button.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnAddButtonClicked(object sender, EventArgs e)
        {
            Type selectedModelType = this.allowableChildFunctions.Find(m => m.FullName == this.view.List.SelectedValue);
            if (selectedModelType != null)
            {
                this.explorerPresenter.MainPresenter.ShowWaitCursor(true);
                try
                {
                    IModel child = (IModel)Activator.CreateInstance(selectedModelType, true);
                    this.explorerPresenter.Add(child, Apsim.FullPath(this.model));
                }
                finally
                {
                    this.explorerPresenter.MainPresenter.ShowWaitCursor(false);
                }
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
                        if (t.FullName == modelName && t.IsPublic && t.IsClass)
                        {
                            modelType = t;
                            break;
                        }
                    }
                }

                if (modelType != null)
                {
                    // Use the pre built serialization assembly.
                    string binDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    string deserializerFileName = Path.Combine(binDirectory, "Models.XmlSerializers.dll");

                    object child = Activator.CreateInstance(modelType, true);
                    string childString = FileFormat.WriteToString(child as IModel);
                    (this.view.List as ListBoxView).SetClipboardText(childString);

                    DragObject dragObject = new DragObject();
                    dragObject.NodePath = e.NodePath;
                    dragObject.ModelType = modelType;
                    dragObject.ModelString = childString;
                    e.DragObject = dragObject;
                }
            }
            finally
            {
                this.explorerPresenter.MainPresenter.ShowWaitCursor(false);
            }
        }

        /// <summary>
        /// The filter/search textbox has been modified by the user.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnFilterChanged(object sender, EventArgs e)
        {
            string filter = view.Filter;
            this.view.List.Values = this.allowableChildFunctions.Where(m => m.Name.IndexOf(filter, StringComparison.InvariantCultureIgnoreCase) >= 0).Select(m => m.FullName).ToArray();
        }
    }
}
