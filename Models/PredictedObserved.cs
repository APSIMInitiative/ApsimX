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
    [ViewName("UserInterface.Views.InputView")]
    [PresenterName("UserInterface.Presenters.ObservedInputPresenter")]
    public class PredictedObserved : Input
    {
        [Link]
        Report Predicted = null;

        public string FieldNameUsedForMatch { get; set; }

        /// <summary>
        /// Simulation has completed. Create a predicted observed data in the data store.
        /// </summary>
        public override void OnCompleted()
        {
            DataTable observedData = GetTable();

            if (observedData != null)
            {
                DataStore dataStore = new DataStore();
                dataStore.Connect(Path.ChangeExtension(Simulation.FileName, ".db"), readOnly: false);
                if (dataStore.TableNames.Contains(Name))
                {
                    // delete the old data.
                    dataStore.DeleteOldContentInTable(Simulation.Name, Name);
                }

                DataTable predictedData = dataStore.GetData(Simulation.Name, Predicted.Name);
                DataTable predictedObservedData = GetPredictedObservedData(dataStore, observedData, predictedData);

                if (predictedObservedData != null)
                    dataStore.WriteTable(Simulation.Name, this.Name, predictedObservedData);
                dataStore.Disconnect();
            }
        }

        /// <summary>
        /// Perform the matching of observed data with predicted data and return the resulting
        /// DataTable. Returns null if no matches.
        /// </summary>
        private DataTable GetPredictedObservedData(DataStore dataStore, 
                                                   DataTable observedData,
                                                   DataTable predictedData)
        {
            if (predictedData != null && observedData != null && 
                predictedData.Columns.Contains(FieldNameUsedForMatch) &&
                observedData.Columns.Contains(FieldNameUsedForMatch))
            {
                DataTable predictedObserved = new DataTable();

                // Add columns to our 'predictedObserved' dataset where there is the same name
                // column in both the predicted and observed data sets.
                foreach (DataColumn observedColumn in observedData.Columns)
                {
                    if (observedColumn.ColumnName != "SimulationName" && predictedData.Columns.Contains(observedColumn.ColumnName))
                    {
                        predictedObserved.Columns.Add("Predicted" + observedColumn.ColumnName, observedColumn.DataType);
                        predictedObserved.Columns.Add("Observed" + observedColumn.ColumnName, observedColumn.DataType);
                    }
                }

                // Go through all rows in 'observedData' and find a single matching row in our
                // 'predictedData' dataset. If found then write a row to our 'predictedObserved' dataset.
                foreach (DataRow observedRow in observedData.Rows)
                {
                    if (!Convert.IsDBNull(observedRow[FieldNameUsedForMatch]))
                    {
                        string filter = "";
                        if (observedData.Columns[FieldNameUsedForMatch].DataType == typeof(DateTime))
                        {
                            DateTime D = Convert.ToDateTime(observedRow[FieldNameUsedForMatch]);
                            filter += FieldNameUsedForMatch + " = #" + D.ToString("MM/dd/yyyy") + "#";
                        }
                        else if (observedData.Columns[FieldNameUsedForMatch].DataType == typeof(string))
                            filter += FieldNameUsedForMatch + " = '" + observedRow[FieldNameUsedForMatch].ToString() + "'";
                        else
                            filter += FieldNameUsedForMatch + " = " + observedRow[FieldNameUsedForMatch].ToString();

                        DataRow[] matchingPredictedRows = predictedData.Select(filter);

                        if (matchingPredictedRows.Length == 1)
                        {
                            DataRow predictedRow = matchingPredictedRows[0];

                            // Create a row in 'predictedObserved'
                            DataRow newRow = predictedObserved.NewRow();

                            // Copy the data from 'predictedData' and 'observedData'
                            foreach (DataColumn observedColumn in observedData.Columns)
                            {
                                if (observedColumn.ColumnName != "SimulationName" && predictedData.Columns.Contains(observedColumn.ColumnName))
                                {
                                    newRow["Predicted" + observedColumn.ColumnName] = predictedRow[observedColumn.ColumnName];
                                    newRow["Observed" + observedColumn.ColumnName] = observedRow[observedColumn.ColumnName];
                                }
                            }

                            predictedObserved.Rows.Add(newRow);
                        }
                    }
                }

                return predictedObserved;
            }
            return null;

        }


    }




}
