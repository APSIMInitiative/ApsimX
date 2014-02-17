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

            DataStoreView.OnTableSelected += OnTableSelected;
            DataStoreView.CreateNowClicked += OnCreateNowClicked;
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
        private void OnTableSelected(string SimulationName, string TableName)
        {
            DataStoreView.PopulateData(DataStore.GetData(SimulationName, TableName));
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
