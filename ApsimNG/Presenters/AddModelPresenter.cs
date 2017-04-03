// -----------------------------------------------------------------------
// <copyright file="AddModelPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using Interfaces;
    using Models.Core;
    using Views;
    using System.IO;
    using System.Reflection;

    /// <summary>This presenter lets the user add a model.</summary>
    public class AddModelPresenter : IPresenter
    {
        /// <summary>The model</summary>
        private IModel model;

        /// <summary>The view</summary>
        private IListButtonView view;

        /// <summary>The parent explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The allowable child models.</summary>
        private List<Type> allowableChildModels;

        /// <summary>Attach the specified Model and View.</summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            this.view = view as IListButtonView;
            this.explorerPresenter = explorerPresenter;

            allowableChildModels = Apsim.GetAllowableChildModels(this.model);
            List<Type> allowableChildFunctions = Apsim.GetAllowableChildFunctions(this.model);
            allowableChildModels.RemoveAll(a => allowableChildFunctions.Any(b => a == b));

            this.view.List.IsModelList = true;
            this.view.List.Values = allowableChildModels.Select(m => m.Name).ToArray();
            this.view.AddButton("Add", null, this.OnAddButtonClicked);

            // Trap events from the view.
            this.view.List.DoubleClicked += this.OnAddButtonClicked;
            this.view.List.DragStarted += this.OnDragStart;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            // Trap events from the view.
            this.view.List.DoubleClicked -= this.OnAddButtonClicked;
            this.view.List.DragStarted -= this.OnDragStart;
        }

        /// <summary>The user has clicked the add button.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnAddButtonClicked(object sender, EventArgs e)
        {
            Type selectedModelType = allowableChildModels.Find(m => m.Name == view.List.SelectedValue);
            if (selectedModelType != null)
            {
                explorerPresenter.MainPresenter.ShowWaitCursor(true);
                try
                {
                    // Use the pre built serialization assembly.
                    string binDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    string deserializerFileName = Path.Combine(binDirectory, "Models.XmlSerializers.dll");

                    object child = Activator.CreateInstance(selectedModelType, true);
                    string childXML = XmlUtilities.Serialise(child, false, deserializerFileName);
                    this.explorerPresenter.Add(childXML, Apsim.FullPath(model));
                    // this.explorerPresenter.HideRightHandPanel();
                }
                finally
                {
                    explorerPresenter.MainPresenter.ShowWaitCursor(false);
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
            explorerPresenter.MainPresenter.ShowWaitCursor(true);
            try
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type t in assembly.GetTypes())
                    {
                        if (t.Name == modelName && t.IsPublic && t.IsClass)
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
                    string childXML = XmlUtilities.Serialise(child, false, deserializerFileName);
                    (view.List as ListBoxView).SetClipboardText(childXML);

                    DragObject dragObject = new DragObject();
                    dragObject.NodePath = e.NodePath;
                    dragObject.ModelType = modelType;
                    dragObject.Xml = childXML;
                    e.DragObject = dragObject;
                }
            }
            finally
            {
                explorerPresenter.MainPresenter.ShowWaitCursor(false);
            }

        }

    }
}
