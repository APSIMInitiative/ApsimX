// -----------------------------------------------------------------------
// <copyright file="InputPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

namespace UserInterface.Presenters
{
    using System;
    using Models;
    using Models.Core;
    using Views;

    /// <summary>
    /// Attaches an Input model to an Input View.
    /// </summary>
    public class InputPresenter : IPresenter
    {
        /// <summary>
        /// The input model
        /// </summary>
        private Models.PostSimulationTools.Input input;

        /// <summary>
        /// The input view
        /// </summary>
        private IInputView view;

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
            this.input = model as Models.PostSimulationTools.Input;
            this.view = view as IInputView;
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
                explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(input, "FullFileName", e.FileName));
            }
            catch (Exception err)
            {
                explorerPresenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>
        /// The model has changed - update the view.
        /// </summary>
        /// <param name="changedModel">The model object</param>
        private void OnModelChanged(object changedModel)
        {
            this.view.FileName = this.input.FullFileName;
            this.view.GridView.DataSource = this.input.GetTable();
            if (this.view.GridView.DataSource == null)
            {
                this.view.WarningText = this.input.ErrorMessage;
            }
            else
            {
                this.view.WarningText = string.Empty;
            }
        }
    }
}
