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
    public class CLEMFileSQLiteGRASPPresenter : IPresenter
    {
        /// <summary>
        /// The filecrop  model
        /// </summary>
        private Models.CLEM.FileSQLiteGRASP model;

        /// <summary>
        /// The filecrop view
        /// </summary>
        private ICLEMFileSQLiteGRASPView view;

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
            this.model = model as Models.CLEM.FileSQLiteGRASP;
            this.view = view as ICLEMFileSQLiteGRASPView;
            this.explorerPresenter = explorerPresenter;
            this.view.BrowseButtonClicked += this.OnBrowseButtonClicked;
            this.view.BackButtonClicked += this.OnBackButtonClicked;
            this.view.NextButtonClicked += this.OnNextButtonClicked;

            this.OnModelChanged(model);  // Updates the view

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detaches an Input model from an Input View.
        /// </summary>
        public void Detach()
        {
            this.view.BrowseButtonClicked -= this.OnBrowseButtonClicked;
            this.view.BackButtonClicked -= this.OnBackButtonClicked;
            this.view.NextButtonClicked -= this.OnNextButtonClicked;
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
                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.model, "FullFileName", e.FileName));
            }
            catch (Exception err)
            {
                this.explorerPresenter.MainPresenter.ShowMessage(err.Message, Simulation.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// Next button was clicked by user.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">The params</param>
        private void OnNextButtonClicked(object sender, EventArgs e)
        {
            try
            {
                int newvalue = this.model.StartYearForGrid + this.model.NumberOfYearsToDisplayInGrid;
                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.model, "StartYearForGrid", newvalue));
            }
            catch (Exception err)
            {
                this.explorerPresenter.MainPresenter.ShowMessage(err.Message, Simulation.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// Back button was clicked by user.
        /// </summary>
        /// <param name="sender">Sender object</param>
        /// <param name="e">The params</param>
        private void OnBackButtonClicked(object sender, EventArgs e)
        {
            try
            {
                int newvalue = this.model.StartYearForGrid - this.model.NumberOfYearsToDisplayInGrid;
                this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.model, "StartYearForGrid", newvalue));
            }
            catch (Exception err)
            {
                this.explorerPresenter.MainPresenter.ShowMessage(err.Message, Simulation.ErrorLevel.Error);
            }
        }


        /// <summary>
        /// The model has changed - update the view.
        /// ie.  Commands.ChangeProperty() has been called. 
        /// </summary>
        /// <param name="changedModel">The model object</param>
        private void OnModelChanged(object changedModel)
        {
            this.view.FileName = this.model.FullFileName;
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
