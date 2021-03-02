namespace UserInterface.Presenters
{
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
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
            PopulateGrid();
        }

        /// <summary>Detach the model from the view.</summary>
        public void Detach()
        {
        }

        /// <summary>Populate the grid control with data.</summary>
        public void PopulateGrid()
        {
            using (DataTable data = GetData())
            {
                if (data != null)
                {
                    // get unique rows
                    List<string> activities = data.AsEnumerable().Select(a => a.Field<string>("UniqueID")).Distinct().ToList<string>();
                    string timeStepUID = data.AsEnumerable().Where(a => a.Field<string>("Name") == "TimeStep").FirstOrDefault().Field<string>("UniqueID");

                    // get unique columns
                    List<DateTime> dates = data.AsEnumerable().Select(a => a.Field<DateTime>("Date")).Distinct().ToList<DateTime>();

                    // create table
                    DataTable tbl = new DataTable();
                    tbl.Columns.Add("Activity");
                    foreach (var item in dates)
                    {
                        tbl.Columns.Add(item.Month.ToString("00") + "\n" + item.ToString("yy"));
                    }
                    // add blank column for resize row height of pixelbuf with font size change
                    tbl.Columns.Add(" ");

                    foreach (var item in activities)
                    {
                        if (item != timeStepUID)
                        {
                            DataRow dr = tbl.NewRow();
                            string name = data.AsEnumerable().Where(a => a.Field<string>("UniqueID") == item).FirstOrDefault()["Name"].ToString();
                            dr["Activity"] = name;

                            foreach (var activityTick in data.AsEnumerable().Where(a => a.Field<string>("UniqueID") == item))
                            {
                                DateTime dte = (DateTime)activityTick["Date"];
                                string status = activityTick["Status"].ToString();
                                dr[dte.Month.ToString("00") + "\n" + dte.ToString("yy")] = status;
                            }
                            dr[" "] = " ";
                            tbl.Rows.Add(dr);
                        }
                    }
                    this.Grid.DataSource = tbl;
                    this.Grid.LockLeftMostColumns(1);  // lock activity name.
                }
            }
        }

        /// <summary>Get data to show in grid.</summary>
        /// <returns>A data table of all data.</returns>
        private DataTable GetData()
        {
            DataTable data = null;
            if (dataStore != null)
            {
                try
                {
                    int count = Utility.Configuration.Settings.MaximumRowsOnReportGrid;
                    data = dataStore.Reader.GetData(
                                            tableName: ModelName,
                                            count: Utility.Configuration.Settings.MaximumRowsOnReportGrid);

                    if(data != null)
                    {
                        // need to filter by current simulation
                        var filteredData = data.AsEnumerable()
                            .Where(row => row.Field<String>("SimulationName") == this.SimulationName & row.Field<String>("Zone") == this.ZoneName);
                        if (filteredData.Any())
                        {
                            data = filteredData.CopyToDataTable();
                        }
                    }

                }
                catch (Exception e)
                {
                    this.explorerPresenter.MainPresenter.ShowError(e);
                }
            }
            else
            {
                data = new DataTable();
            }

            return data;
        }

        /// <summary>The selected table has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnTableSelected(object sender, EventArgs e)
        {
            PopulateGrid();
        }

        /// <summary>The column filter has changed.</summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="e">Event arguments</param>
        private void OnColumnFilterChanged(object sender, EventArgs e)
        {
            PopulateGrid();
        }
    }
}
