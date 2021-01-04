namespace UserInterface.Presenters
{
    using Models;
    using Models.CLEM;
    using Models.CLEM.Reporting;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Views;

    /// <summary>A data store presenter connecting a data store model with a data store view</summary>
    public class ActivityLedgerGridPresenter : IPresenter
    {
        /// <summary>The data store model to work with.</summary>
        private IDataStore dataStore;

        /// <summary>The display grid store view to work with.</summary>
        public IActivityLedgerGridView Grid { get; set; }

        /// <summary>Parent explorer presenter.</summary>
        private ExplorerPresenter explorerPresenter;

        public string ModelName { get; set; }

        public Report ModelReport { get; set; }

        /// <summary>
        /// The name of the simulation to display
        /// </summary>
        public string SimulationName { get; set; }

        /// <summary>
        /// The name of the simulation to display
        /// </summary>
        public string ZoneName { get; set; }

        /// <summary>Attach the model and view to this presenter and populate the view.</summary>
        /// <param name="model">The data store model to work with.</param>
        /// <param name="view">Data store view to work with.</param>
        /// <param name="explorerPresenter">Parent explorer presenter.</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            dataStore = model as IDataStore;
            this.Grid = view as ActivityLedgerGridView;
            this.explorerPresenter = explorerPresenter;
            this.Grid.ReadOnly = true;
        }

        public void Refresh()
        {
            // now get report model to create data as we need to generate the HTML report independent of ApsimNG
            Grid.DataSource = (ModelReport as ReportActivitiesPerformed).CreateDataTable(dataStore, Path.GetDirectoryName(this.explorerPresenter.ApsimXFile.FileName), !Utility.Configuration.Settings.DarkTheme);
            this.Grid.LockLeftMostColumns(1);  // lock activity name.
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
        }

        /// <summary>The selected table has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnTableSelected(object sender, EventArgs e)
        {
            Grid.DataSource = (ModelReport as ReportActivitiesPerformed).CreateDataTable(dataStore, Path.GetDirectoryName(this.explorerPresenter.ApsimXFile.FileName), !Utility.Configuration.Settings.DarkTheme);
            //PopulateGrid();
        }

        /// <summary>The column filter has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnColumnFilterChanged(object sender, EventArgs e)
        {
            Grid.DataSource = (ModelReport as ReportActivitiesPerformed).CreateDataTable(dataStore, Path.GetDirectoryName(this.explorerPresenter.ApsimXFile.FileName), !Utility.Configuration.Settings.DarkTheme);
            //PopulateGrid();
        }
    }
}
