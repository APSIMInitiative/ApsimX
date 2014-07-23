// -----------------------------------------------------------------------
// <copyright file="DataStorePresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using System;
    using Interfaces;
    using Models;
    using Models.Core;
    using System.IO;

    /// <summary>
    /// A data store presenter connecting a data store model with a data store view
    /// </summary>
    public class DataStorePresenter : IPresenter
    {
        /// <summary>
        /// The data store model to work with.
        /// </summary>
        private DataStore dataStore;

        /// <summary>
        /// The data store view to work with.
        /// </summary>
        private IDataStoreView dataStoreView;

        /// <summary>
        /// Parent explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the model and view to this presenter and populate the view.
        /// </summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="view">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.dataStore = model as DataStore;
            this.dataStoreView = view as IDataStoreView;
            this.explorerPresenter = explorerPresenter;

            this.dataStoreView.OnTableSelected += this.OnTableSelected;
            this.dataStoreView.CreateNowClicked += this.OnCreateNowClicked;
            this.dataStoreView.RunChildModelsClicked += this.OnRunChildModelsClicked;
            this.dataStoreView.OnSimulationSelected += this.OnSimulationSelected;

            this.dataStoreView.Grid.ReadOnly = true;
            this.dataStoreView.Grid.AutoFilterOn = true;
            this.dataStoreView.Grid.FloatingPointFormat = "N3";
            this.dataStoreView.TableNames = this.dataStore.TableNames;
            this.dataStoreView.SimulationNames = this.dataStore.SimulationNames;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.dataStoreView.OnTableSelected -= this.OnTableSelected;
            this.dataStoreView.CreateNowClicked -= this.OnCreateNowClicked;
            this.dataStoreView.RunChildModelsClicked -= this.OnRunChildModelsClicked;
            this.dataStoreView.OnSimulationSelected += this.OnSimulationSelected;
        }

        /// <summary>
        /// The selected table has changed.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnTableSelected(object sender, EventArgs e)
        {
            this.dataStoreView.Grid.DataSource = this.dataStore.GetData("*", this.dataStoreView.SelectedTableName);
        }

        /// <summary>
        /// The selected simulation has changed.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnSimulationSelected(object sender, EventArgs e)
        {
            StringWriter writer = new StringWriter();

            Summary.WriteReport(dataStore, this.dataStoreView.SelectedSimulationName, writer, null, true);
            this.dataStoreView.ShowSummaryContent(writer.ToString());
        }

        /// <summary>
        /// Create now has been clicked.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnCreateNowClicked(object sender, EventArgs e)
        {
            this.dataStore.WriteOutputFile();
        }

        /// <summary>
        /// User has clicked the run child models button.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnRunChildModelsClicked(object sender, EventArgs e)
        {
            try
            {
                // Run all child model post processors.
                this.dataStore.RunPostProcessingTools();
            }
            catch (Exception err)
            {
                this.explorerPresenter.ShowMessage("Error: " + err.Message, Models.DataStore.ErrorLevel.Error);
            }

            this.dataStoreView.TableNames = this.dataStore.TableNames;
            this.explorerPresenter.ShowMessage("DataStore post processing models have completed", Models.DataStore.ErrorLevel.Information);
        }
    }
}
