// -----------------------------------------------------------------------
// <copyright file="SummaryPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
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

                // populate the view
                this.SetHtmlInView();

                // subscribe to the simulation name changed event.
                this.view.SimulationNameChanged += this.OnSimulationNameChanged;
            }
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
            this.view.SimulationNameChanged -= this.OnSimulationNameChanged;
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
    }
}