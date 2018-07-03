// -----------------------------------------------------------------------
// <copyright file="SummaryPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
using EventArguments;

namespace UserInterface.Presenters
{
    using System;
    using System.IO;
    using System.Linq;
    using Models;
    using Models.Core;
    using Models.Factorial;
    using Views;

    /// <summary>Presenter class for working with HtmlView</summary>
    public class SummaryPresenter : IPresenter
    {
        /// <summary>The summary model to work with.</summary>
        private Summary summary;

        /// <summary>The view model to work with.</summary>
        private ISummaryView view;

        /// <summary>The explorer presenter which manages this presenter.</summary>
        private ExplorerPresenter presenter;

        /// <summary>Our data store</summary>
        [Link]
        private IStorageReader dataStore = null;

        /// <summary>Attach the model to the view.</summary>
        /// <param name="model">The model to work with</param>
        /// <param name="view">The view to attach to</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.summary = model as Summary;
            this.presenter = explorerPresenter;
            this.view = view as ISummaryView;
            // populate the simulation names in the view.
            Simulation simulation = Apsim.Parent(this.summary, typeof(Simulation)) as Simulation;
            if (simulation != null)
            {
                if (simulation.Parent is Experiment)
                {
                    Experiment experiment = simulation.Parent as Experiment;
                    string[] simulationNames = experiment.GetSimulationNames().ToArray();
                    this.view.SimulationNames = simulationNames;
                    if (simulationNames.Length > 0)
                    {
                        this.view.SimulationName = simulationNames[0];
                    }
                }
                else
                {
                    this.view.SimulationNames = new string[] { simulation.Name };
                    this.view.SimulationName = simulation.Name;
                }

                // Populate the view.
                this.SetHtmlInView();

                // Subscribe to the simulation name changed event.
                this.view.SimulationNameChanged += this.OnSimulationNameChanged;

                // Subscribe to the view's copy event.
                this.view.Copy += OnCopy;
            }
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.view.SimulationNameChanged -= this.OnSimulationNameChanged;
            this.view.Copy -= OnCopy;
        }

        /// <summary>Populate the summary view.</summary>
        private void SetHtmlInView()
        {
            StringWriter writer = new StringWriter();
            Summary.WriteReport(this.dataStore, this.view.SimulationName, writer, Utility.Configuration.Settings.SummaryPngFileName, outtype: Summary.OutputType.html);
            this.view.SetSummaryContent(writer.ToString());
            writer.Close();
        }

        /// <summary>Handles the SimulationNameChanged event of the view control.</summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnSimulationNameChanged(object sender, EventArgs e)
        {
            this.SetHtmlInView();
        }

        /// <summary>
        /// Event handler for the view's copy event.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCopy(object sender, CopyEventArgs e)
        {
            this.presenter.SetClipboardText(e.Text, "CLIPBOARD");
        }
    }
}