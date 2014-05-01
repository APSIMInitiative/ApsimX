using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.IO;
using System.Data;
using System.Xml.Serialization;

namespace Models
{


    /// <summary>
    /// Reads the contents of a file (in apsim format) and stores into the DataStore. 
    /// If the file has a column name of 'SimulationName' then this model will only input data for those rows
    /// where the data in column 'SimulationName' matches the name of the simulation under which
    /// this input model sits. 
    /// 
    /// If the file does NOT have a 'SimulationName' column then all data will be input.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PredictedObservedView")]
    [PresenterName("UserInterface.Presenters.PredictedObservedPresenter")]
    public class PredictedObserved : Model
    {
        public string PredictedTableName { get; set; }
        public string ObservedTableName { get; set; }

        public string FieldNameUsedForMatch { get; set; }


        /// <summary>
        /// Go find the top level simulations object.
        /// </summary>
        public Simulations Simulations
        {
            get
            {
                Model obj = this;
                while (obj.Parent != null && obj.GetType() != typeof(Simulations))
                    obj = obj.Parent;
                if (obj == null)
                    throw new ApsimXException(FullPath, "Cannot find a root simulations object");
                return obj as Simulations;
            }
        }

        /// <summary>
        /// Simulation has completed. Create a predicted observed data in the data store.
        /// </summary>
        public override void OnAllCompleted()
        {
            if (PredictedTableName != null && ObservedTableName != null)
            {
                DataStore dataStore = new DataStore();
                dataStore.Connect(Path.ChangeExtension(Simulations.FileName, ".db"), readOnly: false);

                dataStore.DeleteTable(this.Name);
                
                DataTable predictedData = dataStore.GetData("*", PredictedTableName);
                DataTable observedData = dataStore.GetData("*", ObservedTableName);

                DataTable predictedObservedData = GetPredictedObservedData();

                if (predictedObservedData != null)
                    dataStore.WriteTable(Simulations.Name, this.Name, predictedObservedData);
                dataStore.Disconnect();
            }
        }

        /// <summary>
        /// Perform the matching of observed data with predicted data and return the resulting
        /// DataTable. Returns null if no matches.
        /// </summary>
        private DataTable GetPredictedObservedData()
        {
            string sql = "SELECT * FROM Input I INNER JOIN Report R USING (SimulationID) WHERE I.'@field' = R.'@field'";
            sql = sql.Replace("@field", FieldNameUsedForMatch);

            Utility.SQLite dbHandler = new Utility.SQLite();
            if (!dbHandler.IsOpen)
                dbHandler.OpenDatabase(Path.ChangeExtension(Simulations.FileName, ".db"), true);

            System.Data.DataTable predictedObserved = dbHandler.ExecuteQuery(sql);

                return predictedObserved;
        }


    }




}
