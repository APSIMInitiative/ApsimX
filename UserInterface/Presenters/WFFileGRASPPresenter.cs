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
    public class WFFileGRASPPresenter : IPresenter
    {
        private Models.WholeFarm.FileGRASP FileGRASP;
        private IWFFileView View;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attaches an Input model to an Input View.
        /// </summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            FileGRASP = model as Models.WholeFarm.FileGRASP;
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
                ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(FileGRASP, "FullFileName", e.FileNames[0]));
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
            View.FileName = FileGRASP.FullFileName;
            View.GridView.DataSource = FileGRASP.GetTable();
            if (View.GridView.DataSource == null)
                View.WarningText = FileGRASP.ErrorMessage;
            else
                View.WarningText = string.Empty;
        }
    }
}
