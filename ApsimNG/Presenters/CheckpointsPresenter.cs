namespace UserInterface.Presenters
{
    using global::UserInterface.Interfaces;
    using Models.Core;
    using Models.Storage;
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using Views;

    /// <summary>This presenter lets the user add/delete checkpoints</summary>
    public class CheckpointsPresenter : IPresenter
    {
        /// <summary>The model</summary>
        private IModel model;

        /// <summary>The view</summary>
        private ViewBase view;

        /// <summary>The parent explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>The checkpoint list on the view.</summary>
        private TreeView checkpointList;

        /// <summary>The add button on the view.</summary>
        private ButtonView addButton;

        /// <summary>The delete button on the view.</summary>
        private ButtonView deleteButton;
        
        /// <summary>The checkpoint list popup menu.</summary>
        private MenuDescriptionArgs popupMenu;

        /// <summary>Storage model</summary>
        private IDataStore storage = null;

        /// <summary>Root node forthe checkpoints name list.</summary>
        private TreeViewNode rootNode;

        /// <summary>Attach the specified Model and View.</summary>
        /// <param name="model">The axis model</param>
        /// <param name="view">The axis view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as IModel;
            this.view = view as ViewBase;
            this.explorerPresenter = explorerPresenter;

            storage = Apsim.Find(this.model, typeof(DataStore)) as DataStore;

            checkpointList = this.view.GetControl<TreeView>("CheckpointList");
            addButton = this.view.GetControl<ButtonView>("AddButton");
            deleteButton = this.view.GetControl<ButtonView>("DeleteButton");

            popupMenu = new MenuDescriptionArgs()
            {
                Name = "Show on graphs?",
                ResourceNameForImage = "empty"
            };
            popupMenu.OnClick += OnCheckpointTicked;
            checkpointList.ContextMenu = new MenuView();
            checkpointList.ContextMenu.Populate(new List<MenuDescriptionArgs>() { popupMenu });

            PopulateList();

            addButton.Clicked += OnAddButtonClicked;
            deleteButton.Clicked += OnDeleteButtonClicked;
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            addButton.Clicked -= OnAddButtonClicked;
            deleteButton.Clicked -= OnDeleteButtonClicked;
            popupMenu.OnClick -= OnCheckpointTicked;
        }

        /// <summary>Populate the checkpoint list control.</summary>
        /// <param name="models"></param>
        private void PopulateList()
        {
            var checkpointNames = storage.Reader.CheckpointNames;
            rootNode = new TreeViewNode()
            {
                Name = "Checkpoints",
                ResourceNameForImage = ExplorerPresenter.GetIconResourceName(typeof(Folder), null, null)
            };

            foreach (var checkpointName in checkpointNames)
            {
                var node = new TreeViewNode()
                {
                    Name = checkpointName,
                    ResourceNameForImage = "ApsimNG.Resources.TreeViewImages.Document.png"
                };
                if (storage.Reader.GetCheckpointShowOnGraphs(checkpointName))
                    node.ResourceNameForImage = "ApsimNG.Resources.TreeViewImages.DocumentCheck.png";

                rootNode.Children.Add(node);
            }

            checkpointList.Populate(rootNode);
            checkpointList.ExpandChildren(".Checkpoints");
        }

        /// <summary>The user has clicked the add button.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnAddButtonClicked(object sender, EventArgs e)
        {
            string checkpointName = Utility.StringEntryForm.ShowDialog(explorerPresenter, "New checkpoint name", 
                                                                       "Enter new checkpoint name:", null);
            if (checkpointName != null)
            {
                bool checkpointExists = storage.Reader.CheckpointNames.Contains(checkpointName);
                bool addCheckpoint = !checkpointExists ||
                                     explorerPresenter.MainPresenter.AskQuestion("A checkpoint with this name already exists. Do you want to overwrite previous checkpoint?") == QuestionResponseEnum.Yes;
                if (addCheckpoint)
                {
                    try
                    {
                        explorerPresenter.MainPresenter.ShowWaitCursor(true);
                        explorerPresenter.ApsimXFile.AddCheckpoint(checkpointName);
                        PopulateList();
                        explorerPresenter.MainPresenter.ShowWaitCursor(false);
                        explorerPresenter.MainPresenter.ShowMessage("Checkpoint created: " + checkpointName, Simulation.MessageType.Information);
                    }
                    catch (Exception err)
                    {
                        explorerPresenter.MainPresenter.ShowError(err);
                    }
                }
            }
        }

        /// <summary>The user has clicked the delete button.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnDeleteButtonClicked(object sender, EventArgs e)
        {
            var checkpointName = checkpointList.SelectedNode.Replace(".Checkpoints.", "");
            if (explorerPresenter.MainPresenter.AskQuestion("Are you sure you want to delete checkpoint " + checkpointName + "?") == QuestionResponseEnum.Yes)
            {
                if (checkpointName != null)
                {
                    try
                    {
                        storage.Writer.DeleteCheckpoint(checkpointName);
                        storage.Reader.Refresh();
                        PopulateList();
                        explorerPresenter.MainPresenter.ShowMessage("Checkpoint deleted", Simulation.MessageType.Information);
                    }
                    catch (Exception err)
                    {
                        explorerPresenter.MainPresenter.ShowError(err);
                    }
                }
            }
        }

        /// <summary>The user has clicked a checkpoint.</summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void OnCheckpointTicked(object sender, EventArgs e)
        {
            var checkpointName = checkpointList.SelectedNode.Replace(".Checkpoints.", "");
            var checkpoint = rootNode.Children.Find(node => node.Name == checkpointName);
    
            storage.Writer.SetCheckpointShowGraphs(checkpoint.Name, !checkpoint.ResourceNameForImage.Contains("Check"));
            storage.Reader.Refresh();
            PopulateList();
        }
    }
}
