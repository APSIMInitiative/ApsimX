// -----------------------------------------------------------------------
// <copyright file="CustomQueryPresenter.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------

using ApsimNG.Views.CLEM;
using Models.Core;
using Models.CLEM.Reporting;
using System;
using System.Data;
using UserInterface.Commands;
using UserInterface.Presenters;
using Models.Storage;

namespace ApsimNG.Presenters.CLEM
{
    class CustomQueryPresenter : IPresenter
    {
        /// <summary>
        /// The CustomQuery object
        /// </summary>
        private CustomQuery query = null;

        /// <summary>
        /// The CustomQueryView used
        /// </summary>
        private CustomQueryView view = null;

        /// <summary>
        /// The ExplorerPresenter
        /// </summary>
        private ExplorerPresenter explorer = null;

        /// <summary>
        /// Attach the model and view to the presenter
        /// </summary>
        /// <param name="model">The model to attach</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The presenter to attach to</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.query = model as CustomQuery;
            this.view = view as CustomQueryView;
            this.explorer = explorerPresenter;

            this.view.OnRunQuery += RunQuery;
            this.view.OnLoadFile += LoadFile;
            this.view.OnWriteTable += WriteTable;

            // If the model contains sql, update the view to display it
            if (!string.IsNullOrEmpty(query.Sql))
            {
                this.view.Sql = query.Sql;
                this.view.Filename = query.Filename;
                this.view.Tablename = query.Tablename;
                RunQuery(this, EventArgs.Empty);
            }
        }        

        /// <summary>
        /// Detach the model from the view
        /// </summary>
        public void Detach()
        {
            view.OnRunQuery -= RunQuery;
            view.OnLoadFile -= LoadFile;
            view.OnWriteTable -= WriteTable;
            view.gridview1.Dispose();
            view.Detach();
            SaveData();
        }

        /// <summary>
        /// Track changes made to the view
        /// </summary>
        private void SaveData()
        {
            ChangeProperty sqlcom = new ChangeProperty(this.query, "Sql", view.Sql);
            explorer.CommandHistory.Add(sqlcom);

            ChangeProperty filecom = new ChangeProperty(this.query, "Filename", view.Filename);
            explorer.CommandHistory.Add(filecom);

            ChangeProperty tablecom = new ChangeProperty(this.query, "Tablename", view.Tablename);
            explorer.CommandHistory.Add(tablecom);
        }

        /// <summary>
        /// Applies the SQL to the DataSource
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RunQuery(object sender, EventArgs e)
        {
            var store = Apsim.Find(query, typeof(IDataStore)) as IDataStore;
            view.gridview1.DataSource = store.Reader.GetDataUsingSql(view.Sql);

            SaveData();
        }

        /// <summary>
        /// Overwrites a table in the data store
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void WriteTable(object sender, CustomQueryView.WriteTableEventArgs args)
        {
            DataTable data = view.gridview1.DataSource;
            data.TableName = args.tablename;

            var store = Apsim.Find(query, typeof(IDataStore)) as IDataStore;
            store.Writer.WriteTable(data);

            SaveData();
        }

        /// <summary>
        /// Tracks changes when a new file is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadFile(object sender, EventArgs e)
        {
            SaveData();
        }
    }
}
