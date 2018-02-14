// -----------------------------------------------------------------------
// <copyright file="CheckpointsPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using APSIM.Shared.Utilities;
    using Interfaces;
    using Models.Core;
    using Views;
    using Models.Storage;

    /// <summary>This presenter lets the user add/delete checkpoints</summary>
    public class CheckpointsPresenter : IPresenter
    {
        /// <summary>The model</summary>
        private IModel model;

        /// <summary>The view</summary>
        private IListButtonView view;

        /// <summary>The parent explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>Storage model</summary>
        private DataStore storage = null;

        /// <summary>Attach the specified Model and View.</summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            this.view = view as IListButtonView;
            this.explorerPresenter = explorerPresenter;

            storage = Apsim.Find(this.model, typeof(DataStore)) as DataStore;

            this.view.List.IsModelList = false;
            this.view.List.Values = storage.Checkpoints().ToArray();
            this.view.AddButton("Add", null, this.OnAddButtonClicked);
            this.view.AddButton("Delete", null, this.OnDeleteButtonClicked);
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
        }

        /// <summary>The user has clicked the add button.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnAddButtonClicked(object sender, EventArgs e)
        {

            if (explorerPresenter.MainPresenter.AskUserForOpenFileName("") == QuestionResponseEnum.)
            Type selectedModelType = this.allowableChildModels.Find(m => m.Name == this.view.List.SelectedValue);
            if (selectedModelType != null)
            {
                this.explorerPresenter.MainPresenter.ShowWaitCursor(true);
                try
                {
                    // Use the pre built serialization assembly.
                    string binDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    string deserializerFileName = Path.Combine(binDirectory, "Models.XmlSerializers.dll");

                    object child = Activator.CreateInstance(selectedModelType, true);
                    string childXML = XmlUtilities.Serialise(child, false, deserializerFileName);
                    this.explorerPresenter.Add(childXML, Apsim.FullPath(this.model));
                    /* this.explorerPresenter.HideRightHandPanel(); */
                }
                finally
                {
                    this.explorerPresenter.MainPresenter.ShowWaitCursor(false);
                }
            }
        }

        /// <summary>The user has clicked the delete button.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnDeleteButtonClicked(object sender, EventArgs e)
        {
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
                    (this.view.List as ListBoxView).SetClipboardText(childXML);

                    DragObject dragObject = new DragObject();
                    dragObject.NodePath = e.NodePath;
                    dragObject.ModelType = modelType;
                    dragObject.Xml = childXML;
                    e.DragObject = dragObject;
                }
            }
            finally
            {
                this.explorerPresenter.MainPresenter.ShowWaitCursor(false);
            }
        }
    }
}
