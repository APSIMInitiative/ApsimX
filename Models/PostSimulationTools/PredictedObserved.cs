

namespace Models.PostSimulationTools
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.Run;
    using Models.Factorial;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// # [Name]
    /// Reads the contents of a file (in apsim format) and stores into the DataStore.
    /// If the file has a column name of 'SimulationName' then this model will only input data for those rows
    /// where the data in column 'SimulationName' matches the name of the simulation under which
    /// this input model sits.
    /// If the file does NOT have a 'SimulationName' column then all data will be input.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(ParentType = typeof(Folder))]
    public class PredictedObserved : Model, IPostSimulationTool
    {
        [Link]
        private IDataStore dataStore = null;

        /// <summary>Gets or sets the name of the predicted table.</summary>
        [Description("Predicted table")]
        [Display(Type = DisplayType.TableName)]
        public string PredictedTableName { get; set; }

        /// <summary>Gets or sets the name of the observed table.</summary>
        [Description("Observed table")]
        [Display(Type = DisplayType.TableName)]
        public string ObservedTableName { get; set; }

        /// <summary>Gets or sets the field name used for match.</summary>
        [Description("Field name to use for matching predicted with observed data")]
        [Display(Type = DisplayType.FieldName)]
        public string FieldNameUsedForMatch { get; set; }

        /// <summary>Gets or sets the second field name used for match.</summary>
        [Description("Second field name to use for matching predicted with observed data (optional)")]
        [Display(Type = DisplayType.FieldName)]
        public string FieldName2UsedForMatch { get; set; }

        /// <summary>Gets or sets the third field name used for match.</summary>
        [Description("Third field name to use for matching predicted with observed data (optional)")]
        [Display(Type = DisplayType.FieldName)]
        public string FieldName3UsedForMatch { get; set; }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            if (PredictedTableName != null && ObservedTableName != null)
            {
                IEnumerable<string> predictedDataNames = dataStore.Reader.ColumnNames(PredictedTableName);
                IEnumerable<string> observedDataNames = dataStore.Reader.ColumnNames(ObservedTableName);

                if (predictedDataNames == null)
                    throw new ApsimXException(this, "Could not find model data table: " + PredictedTableName);

                if (observedDataNames == null)
                    throw new ApsimXException(this, "Could not find observed data table: " + ObservedTableName);

                // get the common columns between these lists of columns
                List<string> commonCols = predictedDataNames.Intersect(observedDataNames).ToList();

                IStorageReader reader = dataStore.Reader;
                string match1ObsShort = reader.BriefColumnName(ObservedTableName, FieldNameUsedForMatch);
                string match2ObsShort = reader.BriefColumnName(ObservedTableName, FieldName2UsedForMatch);
                string match3ObsShort = reader.BriefColumnName(ObservedTableName, FieldName3UsedForMatch);

                string match1PredShort = reader.BriefColumnName(PredictedTableName, FieldNameUsedForMatch);
                string match2PredShort = reader.BriefColumnName(PredictedTableName, FieldName2UsedForMatch);
                string match3PredShort = reader.BriefColumnName(PredictedTableName, FieldName3UsedForMatch);

                StringBuilder query = new StringBuilder("SELECT ");
                for (int i = 0; i < commonCols.Count; i++)
                {
                    string s = commonCols[i];
                    string obsColShort = reader.BriefColumnName(ObservedTableName, s);
                    string predColShort = reader.BriefColumnName(PredictedTableName, s);
                    if (i != 0)
                        query.Append(", ");

                    if (s == FieldNameUsedForMatch || s == FieldName2UsedForMatch || s == FieldName3UsedForMatch)
                        query.Append($"O.[{obsColShort}]");
                    else
                        query.Append($"O.[{obsColShort}] AS [Observed.{obsColShort}], P.[{predColShort}] AS [Predicted.{predColShort}]");
                }

                query.AppendLine();
                query.AppendLine("FROM [" + ObservedTableName + "] O");
                query.AppendLine($"INNER JOIN [{PredictedTableName}] P");
                query.Append($"USING ([SimulationID], [CheckpointID], [{FieldNameUsedForMatch}]");
                if (!string.IsNullOrEmpty(FieldName2UsedForMatch))
                    query.Append($", [{FieldName2UsedForMatch}]");
                if (!string.IsNullOrEmpty(FieldName3UsedForMatch))
                    query.Append($", [{FieldName3UsedForMatch}]");
                query.AppendLine(")");

                int checkpointID = dataStore.Writer.GetCheckpointID("Current");
                query.AppendLine("WHERE [CheckpointID] = " + checkpointID);
                query.Replace("O.[SimulationID] AS [Observed.SimulationID], P.[SimulationID] AS [Predicted.SimulationID]", "O.[SimulationID] AS [SimulationID]");
                query.Replace("O.[CheckpointID] AS [Observed.CheckpointID], P.[CheckpointID] AS [Predicted.CheckpointID]", "O.[CheckpointID] AS [CheckpointID]");

                if (Parent is Folder)
                {
                    // Limit it to particular simulations in scope.
                    List<string> simulationNames = new List<string>();
                    foreach (Experiment experiment in Apsim.FindAll(this, typeof(Experiment)))
                    {
                        var names = experiment.GenerateSimulationDescriptions().Select(s => s.Name);
                        simulationNames.AddRange(names);
                    }

                    foreach (Simulation simulation in Apsim.FindAll(this, typeof(Simulation)))
                        if (!(simulation.Parent is Experiment))
                            simulationNames.Add(simulation.Name);

                    query.Append(" AND O.[SimulationID] in (");
                    foreach (string simulationName in simulationNames)
                    {
                        if (simulationName != simulationNames[0])
                            query.Append(',');
                        query.Append(dataStore.Writer.GetSimulationID(simulationName, null));
                    }
                    query.Append(")");
                }

                DataTable predictedObservedData = reader.GetDataUsingSql(query.ToString());

                if (predictedObservedData != null)
                {
                    foreach (DataColumn column in predictedObservedData.Columns)
                    {
                        if (column.ColumnName.StartsWith("Predicted."))
                        {
                            string shortName = column.ColumnName.Substring("Predicted.".Length);
                            column.ColumnName = "Predicted." + reader.FullColumnName(PredictedTableName, shortName);
                        }
                        else if (column.ColumnName.StartsWith("Observed."))
                        {
                            string shortName = column.ColumnName.Substring("Observed.".Length);
                            column.ColumnName = "Observed." + reader.FullColumnName(ObservedTableName, shortName);
                        }
                        else if (column.ColumnName.Equals(match1ObsShort) || column.ColumnName.Equals(match2ObsShort) || column.ColumnName.Equals(match3ObsShort))
                        {
                            column.ColumnName = reader.FullColumnName(ObservedTableName, column.ColumnName);
                        }
                    }

                    // Add in error columns for each data column.
                    foreach (string columnName in commonCols)
                    {
                        if (predictedObservedData.Columns.Contains("Predicted." + columnName) &&
                            predictedObservedData.Columns["Predicted." + columnName].DataType == typeof(double))
                        {
                            var predicted = DataTableUtilities.GetColumnAsDoubles(predictedObservedData, "Predicted." + columnName);
                            var observed = DataTableUtilities.GetColumnAsDoubles(predictedObservedData, "Observed." + columnName);
                            if (predicted.Length > 0 && predicted.Length == observed.Length)
                            {
                                var errorData = MathUtilities.Subtract(predicted, observed);
                                var errorColumnName = "Pred-Obs." + columnName;
                                var errorColumn = predictedObservedData.Columns.Add(errorColumnName, typeof(double));
                                DataTableUtilities.AddColumn(predictedObservedData, errorColumnName, errorData);
                                predictedObservedData.Columns[errorColumnName].SetOrdinal(predictedObservedData.Columns["Predicted." + columnName].Ordinal + 1);
                            }
                        }

                    }

                    // Write table to datastore.
                    predictedObservedData.TableName = this.Name;
                    dataStore.Writer.WriteTable(predictedObservedData);

                    List<string> unitFieldNames = new List<string>();
                    List<string> unitNames = new List<string>();

                    // write units to table.
                    reader.Refresh();

                    foreach (string fieldName in commonCols)
                    {
                        string units = reader.Units(PredictedTableName, fieldName);
                        if (units != null && units != "()")
                        {
                            string unitsMinusBrackets = units.Replace("(", "").Replace(")", "");
                            unitFieldNames.Add("Predicted." + fieldName);
                            unitNames.Add(unitsMinusBrackets);
                            unitFieldNames.Add("Observed." + fieldName);
                            unitNames.Add(unitsMinusBrackets);
                        }
                    }

                    if (unitNames.Count > 0)
                    {
                        // The Writer replaces tables, rather than appends to them,
                        // so we actually need to re-write the existing units table values
                        // Is there a better way to do this?
                        DataView allUnits = new DataView(reader.GetData("_Units"));
                        allUnits.Sort = "TableName";
                        DataTable tableNames = allUnits.ToTable(true, "TableName");
                        foreach (DataRow row in tableNames.Rows)
                        {
                            string tableName = row["TableName"] as string;
                            List<string> colNames = new List<string>();
                            List<string> unitz = new List<string>();
                            foreach (DataRowView rowView in allUnits.FindRows(tableName))
                            {
                                colNames.Add(rowView["ColumnHeading"].ToString());
                                unitz.Add(rowView["Units"].ToString());
                            }
                            dataStore.Writer.AddUnits(tableName, colNames, unitz);
                        }
                        dataStore.Writer.AddUnits(Name, unitFieldNames, unitNames);
                    }
                }
                else
                {
                    // Determine what went wrong.
                    DataTable predictedData = reader.GetDataUsingSql("SELECT * FROM [" + PredictedTableName + "]");
                    DataTable observedData = reader.GetDataUsingSql("SELECT * FROM [" + ObservedTableName + "]");
                    if (predictedData == null || predictedData.Rows.Count == 0)
                        throw new Exception(Name + ": Cannot find any predicted data.");
                    else if (observedData == null || observedData.Rows.Count == 0)
                        throw new Exception(Name + ": Cannot find any observed data in node: " + ObservedTableName + ". Check for missing observed file or move " + ObservedTableName + " to top of child list under DataStore (order is important!)");
                    else
                        throw new Exception(Name + ": Observed data was found but didn't match the predicted values. Make sure the values in the SimulationName column match the simulation names in the user interface. Also ensure column names in the observed file match the APSIM report column names.");
                }
            }
        }
    }
}
