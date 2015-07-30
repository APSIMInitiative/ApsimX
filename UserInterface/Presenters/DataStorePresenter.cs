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
    using System.Collections.Generic;

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

            this.dataStoreView.AutoExport = dataStore.AutoExport;

            this.dataStoreView.OnTableSelected += this.OnTableSelected;
            this.dataStoreView.AutoExportClicked += OnAutoExportClicked;

            this.dataStoreView.Grid.ReadOnly = true;

            this.dataStoreView.Grid.NumericFormat = "N3";
            this.dataStoreView.TableNames = this.GetTableNames();

            this.dataStoreView.Grid.ResizeControls();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.dataStoreView.OnTableSelected -= this.OnTableSelected;
            this.dataStoreView.AutoExportClicked -= OnAutoExportClicked;
        }

        /// <summary>
        /// Get a list of table names to send to the view.
        /// </summary>
        /// <returns>The list of table names</returns>
        private string[] GetTableNames()
        {
            List<string> tableNames = new List<string>();
            foreach (string tableName in this.dataStore.TableNames)
            {
                if (tableName != "Messages" && tableName != "InitialConditions")
                {
                    tableNames.Add(tableName);
                }
            }

            return tableNames.ToArray();
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
        /// Create now has been clicked.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnExportNowClicked(object sender, EventArgs e)
        {
            this.dataStore.WriteToTextFiles();

            // Tell user all is done.
            this.explorerPresenter.ShowMessage("Files created successfully", DataStore.ErrorLevel.Information);
        }

        /// <summary>
        /// The auto export option has been changed.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnAutoExportClicked(object sender, EventArgs e)
        {
            Commands.ChangeProperty command = new Commands.ChangeProperty(this.dataStore, "AutoExport", this.dataStoreView.AutoExport);
            this.explorerPresenter.CommandHistory.Add(command);
        }

        /// <summary>
        /// Write the summary report to a file
        /// </summary>
        /// <param name="baseline">Indicates whether the baseline data store should be used.</param>
        private void WriteSummaryFiles(DataStore dataStoreToUse)
        {
            string fileName = dataStoreToUse.Filename + ".sum";

            StreamWriter report = report = new StreamWriter(fileName);
            foreach (string simulationName in dataStoreToUse.SimulationNames)
            {
                Summary.WriteReport(dataStoreToUse, simulationName, report, null, outtype: Summary.OutputType.html);
                report.WriteLine();
                report.WriteLine();
                report.WriteLine("############################################################################");
            }
            report.Close();
        }
    }
}
