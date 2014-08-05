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
        private IHTMLView view;

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
            this.view = view as IHTMLView;
            this.explorerPresenter = explorerPresenter;
            this.PopulateView();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
        }

        /// <summary>
        /// Populate the summary view.
        /// </summary>
        private void PopulateView()
        {
            Utility.Configuration configuration = new Utility.Configuration();

            // Get a simulation object.
            Simulation simulation = this.summary.ParentOfType(typeof(Simulation)) as Simulation;

            DataStore dataStore = new DataStore(simulation, false);

            StringWriter writer = new StringWriter();
            Summary.WriteReport(dataStore, simulation.Name, writer, configuration.SummaryPngFileName, html:true);
            this.view.MemoText = writer.ToString();

            dataStore.Disconnect();
            writer.Close();
        }
    }
}