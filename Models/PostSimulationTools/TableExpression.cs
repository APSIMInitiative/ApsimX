using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models.PostSimulationTools
{

    /// <summary>
    /// This is a post simulation tool that lets the user write an expression using 
    /// simulation names from a source datatable to produce a new dataset to store in the datastore.
    /// </summary>
    /// <remarks>
    /// For example There are two simulations (ambient and elevated co2) in a table.
    /// Calculate a new table that shows the difference in growth between the two situations
    /// expressed as a percentage (ELEV-AMB)/AMB is XX%
    /// The expression string for this would be:
    ///     (ElevatedCO2-AmbientCO2)/AmbientCO2*100
    /// where ElevatedCO2 and AmbientCO2 are the names of the simulations in the
    /// source data table.
    /// This model matches dates so it assumes that there is a Clock.Today field.
    /// </remarks>
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    [ValidParent(typeof(ParallelPostSimulationTool))]
    [ValidParent(ParentType = typeof(SerialPostSimulationTool))]
    [Serializable]
    public class TableExpression : Model, IPostSimulationTool
    {
        /// <summary>Link to datastore</summary>
        [Link]
        private IDataStore dataStore = null;

        /// <summary>The name of the source table name.</summary>
        [Description("Name of source table")]
        [Display(Type = DisplayType.TableName)]
        public string SourceTableName { get; set; }

        /// <summary>The expression.</summary>
        [Description("Expression (using simulation names)")]
        [Display]
        public string Expression { get; set; }


        /// <summary>The field name to match on.</summary>
        [Description("The field name to match on")]
        [Display]
        public string FieldNameToMatchOn { get; set; }

        /// <summary>Main run method for performing our calculations and storing data.</summary>
        public void Run()
        {
            if (string.IsNullOrEmpty(Expression))
                throw new Exception($"Empty expression found in {Name}");

            if (string.IsNullOrEmpty(FieldNameToMatchOn))
                throw new Exception($"Empty match field name found in {Name}");

            var sourceData = dataStore.Reader.GetData(SourceTableName);
            if (sourceData != null)
            {
                if (!sourceData.Columns.Contains(FieldNameToMatchOn))
                    throw new Exception($"Cannot find field {FieldNameToMatchOn} in table {SourceTableName}");

                var expression = new ExpressionEvaluator();
                expression.Parse(Expression);
                expression.Infix2Postfix();
                FillVariableNamesInExpression(expression, sourceData);
            }
        }

        /// <summary>
        /// Give values from the source data for all variable names in the expression.
        /// </summary>
        /// <param name="expression">The expression evaluator.</param>
        /// <param name="sourceData">The source data.</param>
        private void FillVariableNamesInExpression(ExpressionEvaluator expression, DataTable sourceData)
        {
            var simulationData = new List<Data>();

            foreach (var variable in expression.Variables)
            {
                if (simulationData.Find(data => data.Name == variable.m_name) == null)
                    simulationData.Add(new Data(sourceData, variable.m_name));
            }

            // Need to get a list of common values for the key across the datasets.
            IEnumerable<object> keyValues = null;
            foreach (var dataset in simulationData)
            {
                if (keyValues == null)
                    keyValues = dataset.GetValues(FieldNameToMatchOn);
                else
                    keyValues = keyValues.Intersect(dataset.GetValues(FieldNameToMatchOn));
            }

            // Create a new data set with the key field.
            var newTable = new DataTable(Name);
            DataTableUtilities.AddColumnOfObjects(newTable, FieldNameToMatchOn, keyValues.ToArray());

            // Add all the double fields into the table.
            foreach (DataColumn column in sourceData.Columns)
            {
                if (column.DataType == typeof(double) && column.ColumnName != FieldNameToMatchOn)
                    newTable.Columns.Add(column.ColumnName, typeof(double));
            }

            // Get each dataset to return
            foreach (DataColumn column in sourceData.Columns)
            {
                if (column.DataType == typeof(double) && column.ColumnName != FieldNameToMatchOn)
                {
                    var expressionVariables = new List<Symbol>();
                    foreach (var variable in expression.Variables)
                    {
                        var dataset = simulationData.Find(data => data.Name == variable.m_name);
                        var symbol = new Symbol();
                        symbol.m_name = variable.m_name;
                        symbol.m_values = dataset.GetDoubleValues(column.ColumnName);
                        if (symbol.m_values.Length != keyValues.Count())
                            throw new Exception($"Incorrect number of values for simulation {dataset.Name}");
                        expressionVariables.Add(symbol);
                    }
                    expression.Variables = expressionVariables;

                    expression.EvaluatePostfix();
                    if (expression.Error)
                        throw new Exception($"Error while evaluating expression for column {column.ColumnName}. {expression.ErrorDescription}");

                    // Add data to new data table.
                    DataTableUtilities.AddColumn(newTable, column.ColumnName, expression.Results);
                }
            }

            // Give the new data table to the data store.
            dataStore.Writer.WriteTable(newTable);
        }

        private class Data
        {
            /// <summary>Data filter.</summary>
            private DataView filter;

            /// <summary>Constructor.</summary>
            /// <param name="sourceData">The source data.</param>
            /// <param name="simulationName">The simulation name.</param>
            public Data(DataTable sourceData, string simulationName)
            {
                filter = new DataView(sourceData);
                filter.RowFilter = $"SimulationName='{simulationName}'";
                if (filter.Count == 0)
                    throw new Exception($"No data was found for simulation {simulationName} in table {sourceData.TableName}");
                Name = simulationName;
            }

            public string Name { get; }

            /// <summary>Get a list of values for a field.</summary>
            /// <param name="fieldName">The name of the field.</param>
            public IEnumerable<object> GetValues(string fieldName)
            {
                if (filter.Table.Columns[fieldName].DataType == typeof(DateTime))
                    return DataTableUtilities.GetColumnAsDates(filter, fieldName).Cast<object>();
                else
                    return DataTableUtilities.GetColumnAsStrings(filter, fieldName).Cast<string>();
            }

            /// <summary>Get a list of values for a field.</summary>
            /// <param name="fieldName">The name of the field.</param>
            public double[] GetDoubleValues(string fieldName)
            {
                return DataTableUtilities.GetColumnAsDoubles(filter, fieldName);
            }

        }
    }
}
