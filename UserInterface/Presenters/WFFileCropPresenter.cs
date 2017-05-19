using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Attaches an Input model to an Input View.
    /// </summary>
    public class WFFileCropPresenter : IPresenter
    {
        private Models.WholeFarm.FileAPSIMCrop FileAPSIM;
        private IWFFileView View;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attaches an Input model to an Input View.
        /// </summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            FileAPSIM = model as Models.WholeFarm.FileAPSIMCrop;
            View = view as IWFFileView;
            ExplorerPresenter = explorerPresenter;

            OnModelChanged(model);  // Updates the view

            View.BrowseButtonClicked += OnBrowseButtonClicked;
            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
        }


        /// <summary>
        /// Detaches an Input model from an Input View.
        /// </summary>
        public void Detach()
        {
            View.BrowseButtonClicked -= OnBrowseButtonClicked;
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// Browse button was clicked by user.
        /// </summary>
        private void OnBrowseButtonClicked(object sender, OpenDialogArgs e)
        {
            try
            {
                ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(FileAPSIM, "FullFileName", e.FileNames[0]));
            }
            catch (Exception err)
            {
                ExplorerPresenter.MainPresenter.ShowMessage(err.Message, DataStore.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// The model has changed - update the view.
        /// </summary>
        void OnModelChanged(object changedModel)
        {
            View.FileName = FileAPSIM.FullFileName;
            View.GridView.DataSource = FileAPSIM.GetTable();
            if (View.GridView.DataSource == null)
                View.WarningText = FileAPSIM.ErrorMessage;
            else
                View.WarningText = string.Empty;
        }
    }
}
