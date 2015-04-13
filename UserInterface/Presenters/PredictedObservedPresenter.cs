using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models;
using UserInterface.Views;
using System.IO;
using System.Data;
using APSIM.Shared.Utilities;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Attaches an Input model to an Input View.
    /// </summary>
    public class PredictedObservedPresenter : IPresenter
    {
        private Models.PostSimulationTools.PredictedObserved PredictedObserved;
        private IPredictedObservedView View;
        private ExplorerPresenter ExplorerPresenter;
        private DataStore DataStore;

        /// <summary>
        /// Attaches an Input model to an Input View.
        /// </summary>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            PredictedObserved = model as Models.PostSimulationTools.PredictedObserved;
            View = view as IPredictedObservedView;
            ExplorerPresenter = explorerPresenter;

            // Need a datastore.
            DataStore = new DataStore(PredictedObserved);

            PopulateView();

            ExplorerPresenter.CommandHistory.ModelChanged += OnModelChanged;
            View.ObservedTableNameChanged += OnObservedTableNameChanged;
            View.PredictedTableNameChanged += OnPredictedTableNameChanged;
            View.FieldNameChanged += OnColumnNameChanged;
        }

        /// <summary>
        /// Detaches an Input model from an Input View.
        /// </summary>
        public void Detach()
        {
            DataStore.Disconnect();
            ExplorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            View.ObservedTableNameChanged -= OnObservedTableNameChanged;
            View.PredictedTableNameChanged -= OnPredictedTableNameChanged;
            View.FieldNameChanged -= OnColumnNameChanged;
        }

        /// <summary>
        /// Populate our view.
        /// </summary>
        private void PopulateView()
        {
            View.TableNames = DataStore.TableNames;
            View.PredictedTableName = PredictedObserved.PredictedTableName;
            View.ObservedTableName = PredictedObserved.ObservedTableName;

            DataTable table = DataStore.GetData("*", PredictedObserved.ObservedTableName);
            if (table != null)
            {
                View.FieldNames = DataTableUtilities.GetColumnNames(table);
                View.FieldName = PredictedObserved.FieldNameUsedForMatch;
            }
        }

        /// <summary>
        /// Predicted table name has changed. Set the field in our model.
        /// </summary>
        void OnPredictedTableNameChanged(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(PredictedObserved, "PredictedTableName", View.PredictedTableName));
        }

        /// <summary>
        /// Observed table name has changed. Set the field in our model.
        /// </summary>
        void OnObservedTableNameChanged(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(PredictedObserved, "ObservedTableName", View.ObservedTableName));
        }

        /// <summary>
        /// User has clicked a column header.
        /// </summary>
        void OnColumnNameChanged(object sender, EventArgs e)
        {
            ExplorerPresenter.CommandHistory.Add(new Commands.ChangeProperty(PredictedObserved, "FieldNameUsedForMatch", View.FieldName)); 
        }

        /// <summary>
        /// The model has changed - update the view.
        /// </summary>
        void OnModelChanged(object changedModel)
        {
            PopulateView();
        }
    }
}
