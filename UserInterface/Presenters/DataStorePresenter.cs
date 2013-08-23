using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Core;
using UserInterface.Views;
using System.Data;
using Model.Components;

namespace UserInterface.Presenters
{
    class DataStorePresenter : IPresenter
    {
        private DataStore DataStore;
        private IDataStoreView DataStoreView;

        /// <summary>
        /// Attach the model and view to this presenter and populate the view.
        /// </summary>
        public void Attach(object Model, object View, CommandHistory CommandHistory)
        {
            DataStore = Model as DataStore;
            DataStoreView = View as IDataStoreView;
            
            DataStoreView.PopulateTables(DataStore.SimulationNames, DataStore.TableNames);
            DataStoreView.OnTableSelected += OnTableSelected;
        }

        /// <summary>
        /// The selected table has changed.
        /// </summary>
        private void OnTableSelected(string SimulationName, string TableName)
        {
            DataStoreView.PopulateData(DataStore.GetData(SimulationName, TableName));
        }
    }
}
