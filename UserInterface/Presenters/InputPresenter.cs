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
    public class InputPresenter : IPresenter
    {
        private Models.PostSimulationTools.Input Input;
        private IInputView View;
        private ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attaches an Input model to an Input View.
        /// </summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            Input = model as Models.PostSimulationTools.Input;
            View = view as IInputView;
            ExplorerPresenter = explorerPresenter;

            View.FileName = Input.FullFileName;
            View.GridView.DataSource = Input.GetTable();

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
                ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(Input, "FullFileName", e.FileNames[0]));
            }
            catch (Exception err)
            {
                ExplorerPresenter.ShowMessage(err.Message, DataStore.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// The model has changed - update the view.
        /// </summary>
        void OnModelChanged(object changedModel)
        {
            View.FileName = Input.FullFileName;
            View.GridView.DataSource = Input.GetTable();
        }
    }
}
