using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.IO;
using System.Data;
using System.Xml.Serialization;

namespace Models.PostSimulationTools
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
                
                DataTable predictedDataNames = dataStore.RunQuery("PRAGMA table_info(" + PredictedTableName + ")");
                DataTable observedDataNames  = dataStore.RunQuery("PRAGMA table_info(" + ObservedTableName + ")");

                if (observedDataNames == null)
                {
                    Console.WriteLine("Could not find observed data table: " + ObservedTableName);
                    return;
                }

                IEnumerable<string> commonCols = from p in predictedDataNames.AsEnumerable()
                                               join o in observedDataNames.AsEnumerable() on p["name"] equals o["name"]
                                               select p["name"] as string;

                StringBuilder query = new StringBuilder("SELECT ");
                foreach (string s in commonCols)
                {
                    query.Append("I.'@field' AS 'Observed.@field', R.'@field' AS 'Predicted.@field', ");
                    query.Replace("@field", s);
                }
                
                query.Append("FROM " + ObservedTableName + " I INNER JOIN Report R USING (SimulationID) WHERE I.'@match' = R.'@match'");
                query.Replace(", FROM", " FROM"); // get rid of the last comma
                query.Replace("I.'SimulationID' AS 'Observed.SimulationID', R.'SimulationID' AS 'Predicted.SimulationID'", "I.'SimulationID' AS 'SimulationID'");

                DataTable predictedObservedData = GetPredictedObservedData(query.ToString());

                if (predictedObservedData != null)
                    dataStore.WriteTable(null, this.Name, predictedObservedData);
                dataStore.Disconnect();
            }
        }

        /// <summary>
        /// Perform the matching of observed data with predicted data and return the resulting
        /// DataTable. Returns null if no matches.
        /// </summary>
        private DataTable GetPredictedObservedData(string query)
        {
            Utility.SQLite dbHandler = new Utility.SQLite();
            if (!dbHandler.IsOpen)
                dbHandler.OpenDatabase(Path.ChangeExtension(Simulations.FileName, ".db"), true);

            query = query.Replace("@match", FieldNameUsedForMatch);

            System.Data.DataTable predictedObserved = dbHandler.ExecuteQuery(query);
            dbHandler.CloseDatabase();

            return predictedObserved;
        }


    }




}
