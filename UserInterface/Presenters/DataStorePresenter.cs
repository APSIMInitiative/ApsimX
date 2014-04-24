using System;
using UserInterface.Views;
using Models;
using System.Data;

namespace UserInterface.Presenters
{
    class DataStorePresenter : IPresenter
    {
        private DataStore DataStore;
        private IDataStoreView DataStoreView;
        ExplorerPresenter ExplorerPresenter;

        /// <summary>
        /// Attach the model and view to this presenter and populate the view.
        /// </summary>
        public void Attach(object Model, object View, ExplorerPresenter explorerPresenter)
        {
            DataStore = Model as DataStore;
            DataStoreView = View as IDataStoreView;
            ExplorerPresenter = explorerPresenter;

            DataStoreView.OnTableSelected += OnTableSelected;
            DataStoreView.CreateNowClicked += OnCreateNowClicked;

            DataStoreView.Grid.ReadOnly = true;
            DataStoreView.Grid.AutoFilterOn = true;
            DataStoreView.PopulateTables(DataStore.TableNames);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            DataStoreView.OnTableSelected -= OnTableSelected;
            DataStoreView.CreateNowClicked -= OnCreateNowClicked;
        }

        /// <summary>
        /// The selected table has changed.
        /// </summary>
        private void OnTableSelected(string TableName)
        {
            DataStoreView.Grid.DataSource = DataStore.GetData("*", TableName);

            if (DataStoreView.Grid.DataSource != null)
            {
                // Make all numeric columns have a format of N3
                foreach (DataColumn col in DataStoreView.Grid.DataSource.Columns)
                {
                    DataStoreView.Grid.SetColumnAlignment(col.Ordinal, false);
                    if (col.DataType == typeof(double))
                        DataStoreView.Grid.SetColumnFormat(col.Ordinal, "N3");
                }
            }
        }

        /// <summary>
        /// Create now has been clicked.
        /// </summary>
        void OnCreateNowClicked(object sender, EventArgs e)
        {
            DataStore.WriteOutputFile();
        }

    }
}
