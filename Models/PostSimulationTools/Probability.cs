//-----------------------------------------------------------------------
// <copyright file="Probability.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PostSimulationTools
{
    using System;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using Models.Core;

    /// <summary>
    /// A post processing model that creates a probability table.
    /// </summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Serializable]
    public class Probability : Model, IPostSimulationTool
    {
        /// <summary>
        /// Gets or sets the name of the predicted/observed table name.
        /// </summary>
        [Description("Table name")]
        [Display(DisplayType = DisplayAttribute.DisplayTypeEnum.TableName)]
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Probability"/> is exceedence.
        /// </summary>
        [Description("Probability of exceedence?")]
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        public bool Exceedence { get; set; }

        /// <summary>
        /// The main run method called to fill tables in the specified DataStore.
        /// </summary>
        /// <param name="dataStore">The DataStore to work with</param>
        public void Run(DataStore dataStore)
        {
            dataStore.DeleteTable(this.Name);

            DataTable simulationData = dataStore.GetData("*", this.TableName);
            if (simulationData != null)
            {
                // Add all the necessary columns to our data table.
                DataTable probabilityData = new DataTable();
                probabilityData.Columns.Add("Probability", typeof(double));
                foreach (DataColumn column in simulationData.Columns)
                {
                    if (column.DataType == typeof(double))
                        probabilityData.Columns.Add(column.ColumnName, typeof(double));
                }

                string[] simulationNames = dataStore.SimulationNames;

                DataView view = new DataView(simulationData);
                foreach (string simulationName in simulationNames)
                {
                    view.RowFilter = "SimName = '" + simulationName + "'";

                    int startRow = probabilityData.Rows.Count;

                    // Add in a simulation column.
                    string[] simulationNameColumnValues = Utility.String.CreateStringArray(simulationName, view.Count);
                    Utility.DataTable.AddColumn(probabilityData, "SimulationName", simulationNameColumnValues);

                    // Add in the probability column
                    double[] probabilityValues = Utility.Math.ProbabilityDistribution(view.Count, this.Exceedence);
                    Utility.DataTable.AddColumn(probabilityData, "Probability", probabilityValues, startRow, view.Count);

                    // Add in all other numeric columns.
                    foreach (DataColumn column in simulationData.Columns)
                    {
                        if (column.DataType == typeof(double))
                        {
                            double[] values = Utility.DataTable.GetColumnAsDoubles(view, column.ColumnName);
                            Array.Sort<double>(values);
                            Utility.DataTable.AddColumn(probabilityData, column.ColumnName, values, startRow, values.Length);
                        }
                    }
                }

                // Write the stats data to the DataStore
                dataStore.WriteTable(null, this.Name, probabilityData);
            }
        }
    }
}
