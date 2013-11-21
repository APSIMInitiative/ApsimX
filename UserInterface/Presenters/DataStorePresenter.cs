using System;
using UserInterface.Views;
using Models;

namespace UserInterface.Presenters
{
    class DataStorePresenter : IPresenter
    {
        private DataStore DataStore;
        private IDataStoreView DataStoreView;
        CommandHistory CommandHistory;

        /// <summary>
        /// Attach the model and view to this presenter and populate the view.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory commandHistory)
        {
            DataStore = Model as DataStore;
            DataStoreView = View as IDataStoreView;
            CommandHistory = commandHistory;
            
            DataStoreView.PopulateTables(DataStore.SimulationNames, DataStore.TableNames);
            DataStoreView.AutoCreate = DataStore.AutoCreateReport;

            DataStoreView.OnTableSelected += OnTableSelected;
            DataStoreView.AutoCreateChanged += OnAutoCreateChanged;
            DataStoreView.CreateNowClicked += OnCreateNowClicked;
            CommandHistory.ModelChanged += OnModelChanged;
        }



        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            DataStoreView.OnTableSelected -= OnTableSelected;
            DataStoreView.AutoCreateChanged -= OnAutoCreateChanged;
            DataStoreView.CreateNowClicked -= OnCreateNowClicked;
            CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// The selected table has changed.
        /// </summary>
        private void OnTableSelected(string SimulationName, string TableName)
        {
            DataStoreView.PopulateData(DataStore.GetData(SimulationName, TableName));
        }

        /// <summary>
        /// Create now has been clicked.
        /// </summary>
        void OnCreateNowClicked(object sender, EventArgs e)
        {
            DataStore.CreateReport(false);
            DataStore.Disconnect();
            DataStore.Connect(true);
            DataStore.CreateReport(true);
            DataStore.Disconnect();
        }

        /// <summary>
        /// The auto create checkbox has been changed.
        /// </summary>
        void OnAutoCreateChanged(object sender, EventArgs e)
        {
            CommandHistory.Add(new Commands.ChangePropertyCommand(DataStore, "AutoCreateReport", DataStoreView.AutoCreate));
        }

        void OnModelChanged(object changedModel)
        {
            if (changedModel == DataStore)
            {
                DataStoreView.AutoCreate = DataStore.AutoCreateReport;
            }
        }
    }
}
