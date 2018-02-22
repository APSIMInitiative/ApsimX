using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models;
using Models.Core;
using UserInterface.Views;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Attaches an Input model to an Input View.
    /// </summary>
    public class WFFileCropPresenter : IPresenter
    {
        private Models.WholeFarm.FileCrop FileCrop;
        private IWFFileView View;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attaches an Input model to an Input View.
        /// </summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            FileCrop = model as Models.WholeFarm.FileCrop;
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
                ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(FileCrop, "FullFileName", e.FileNames[0]));
            }
            catch (Exception err)
            {
                ExplorerPresenter.MainPresenter.ShowMessage(err.Message, Simulation.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// The model has changed - update the view.
        /// </summary>
        void OnModelChanged(object changedModel)
        {
            View.FileName = FileCrop.FullFileName;
            View.GridView.DataSource = FileCrop.GetTable();
            if (View.GridView.DataSource == null)
                View.WarningText = FileCrop.ErrorMessage;
            else
                View.WarningText = string.Empty;
        }
    }
}
