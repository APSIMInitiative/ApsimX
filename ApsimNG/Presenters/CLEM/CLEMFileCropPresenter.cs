
namespace UserInterface.Presenters
{
    using System;
    using System.IO;
    using Models;
    using Models.Core;
    using Views;

    /// <summary>
    /// Attaches an Input model to an Input View.
    /// </summary>
    public class CLEMFileCropPresenter : IPresenter
    {
        /// <summary>
        /// The filecrop  model
        /// </summary>
        private Models.CLEM.FileCrop model;

        /// <summary>
        /// The filecrop view
        /// </summary>
        private ICLEMFileCropView view;

        /// <summary>
        /// The Explorer
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attaches an Input model to an Input View.
        /// </summary>
        /// <param name="model">The model to attach</param>
        /// <param name="view">The View to attach</param>
        /// <param name="explorerPresenter">The explorer</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = model as Models.CLEM.FileCrop;
            this.view = view as ICLEMFileCropView;
            this.explorerPresenter = explorerPresenter;
            this.view.BrowseButtonClicked += this.OnBrowseButtonClicked;

            this.OnModelChanged(model);  // Updates the view

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detaches an Input model from an Input View.
        /// </summary>
        public void Detach()
        {
            this.view.BrowseButtonClicked -= this.OnBrowseButtonClicked;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }

        /// <summary>
        /// Browse button was clicked by user.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">The params</param>
        private void OnBrowseButtonClicked(object sender, OpenDialogArgs e)
        {
            try
            {
                //Dialogs seem to return the full file path not just the filename.
                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.model, "FullFileName", e.FileName));
            }
            catch (Exception err)
            {
                this.explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The model has changed - update the view.
        /// </summary>
        /// <param name="changedModel">The model object</param>
        private void OnModelChanged(object changedModel)
        {
            this.view.FileName = this.model.FileName; //set the views filename to the relative filename path not the absolute filename path.
            this.view.GridView.DataSource = this.model.GetTable();
            if (this.view.GridView.DataSource == null)
            {
                this.view.WarningText = this.model.ErrorMessage;
            }
            else
            {
                this.view.WarningText = string.Empty;
            }
        }
    }
}
