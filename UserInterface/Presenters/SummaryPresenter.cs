// -----------------------------------------------------------------------
// <copyright file="SummaryPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using System.IO;
    using Models;
    using Models.Core;
    using Views;

    /// <summary>
    /// Presenter class for working with HtmlView
    /// </summary>
    public class SummaryPresenter : IPresenter
    {
        /// <summary>
        /// The summary model to work with.
        /// </summary>
        private Summary summary;

        /// <summary>
        /// The view model to work with.
        /// </summary>
        private ISummaryView view;

        /// <summary>
        /// The parent explorer presenter
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to work with</param>
        /// <param name="view">The view to attach to</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.summary = model as Summary;
            this.view = view as ISummaryView;
            this.explorerPresenter = explorerPresenter;
            this.PopulateView();

            this.view.AutoCreateChanged += this.OnAutoCreateChanged;
            this.view.CreateButtonClicked += this.OnCreateButtonClicked;
            this.view.HTMLChanged += this.OnHTMLChanged;
            this.view.StateVariablesChanged += this.OnStateVariablesChanged;
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.view.AutoCreateChanged -= this.OnAutoCreateChanged;
            this.view.CreateButtonClicked -= this.OnCreateButtonClicked;
            this.view.HTMLChanged -= this.OnHTMLChanged;
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
        }

        /// <summary>
        /// Populate the summary view.
        /// </summary>
        private void PopulateView()
        {
            this.view.AutoCreate = this.summary.AutoCreate;
            this.view.html = this.summary.Html;

            Utility.Configuration configuration = new Utility.Configuration();

            // Get a simulation object.
            Simulation simulation = this.summary.ParentOfType(typeof(Simulation)) as Simulation;

            DataStore dataStore = new DataStore(simulation, false);

            StringWriter writer = new StringWriter();
            Summary.WriteReport(dataStore, simulation.Name, writer, configuration.SummaryPngFileName, this.summary.Html);
            this.view.SetSummary(writer.ToString(), this.summary.Html);
        }

        /// <summary>
        /// User has changed the HTML state.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnHTMLChanged(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.summary, "html", this.view.html));
        }

        /// <summary>
        /// User has changed the auto create state.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnAutoCreateChanged(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.summary, "AutoCreate", this.view.AutoCreate));
        }

        /// <summary>
        /// User has changed the state variables state.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnStateVariablesChanged(object sender, EventArgs e)
        {
            this.explorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(this.summary, "StateVariables", this.view.StateVariables));
        }

        /// <summary>
        /// User has clicked the create button.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCreateButtonClicked(object sender, EventArgs e)
        {
            this.summary.WriteReportToFile(baseline: false);
            this.summary.WriteReportToFile(baseline: true);
        }

        /// <summary>
        /// The model has changed probably because of undo/redo.
        /// </summary>
        /// <param name="changedModel">The model that has changed</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == this.summary)
            {
                this.PopulateView();
            }
        }
    }
}