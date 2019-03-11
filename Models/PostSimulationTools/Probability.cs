namespace Models.PostSimulationTools
{
    using System;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using Models.Core;
    using APSIM.Shared.Utilities;
    using Storage;
    using System.Linq;
    using Models.Core.Run;

    /// <summary>
    /// # [Name]
    /// A post processing model that creates a probability table.
    /// </summary>
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType=typeof(DataStore))]
    [Serializable]
    public class Probability : Model, IPostSimulationTool
    {
        /// <summary>
        /// Gets or sets the name of the predicted/observed table name.
        /// </summary>
        [Description("Table name")]
        [Display(Type = DisplayType.TableName)]
        public string TableName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Probability"/> is exceedence.
        /// </summary>
        [Description("Probability of exceedence?")]
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        public bool Exceedence { get; set; }

        /// <summary>
        /// The field name the probability function should split series on.
        /// </summary>
        [Description("Field name to split series on")]
        [Display(Type = DisplayType.FieldName)]
        public string FieldToSplitOn { get; set; } = "SimulationName";

        /// <summary>
        /// The main run method called to fill tables in the specified DataStore.
        /// </summary>
        /// <param name="dataStore">The DataStore to work with</param>
        public void Run(IDataStore dataStore)
        {
            DataTable simulationData = dataStore.Reader.GetData(TableName, fieldNames: dataStore.Reader.ColumnNames(TableName));
            if (simulationData != null)
            {
                IndexedDataTable simData = new IndexedDataTable(simulationData, new string[] { FieldToSplitOn });
                IndexedDataTable probabilityData = new IndexedDataTable(new string[] { FieldToSplitOn });

                foreach (var group in simData.Groups())
                {
                    object keyValue = group.IndexValues[0];

                    // Add in our key column
                    probabilityData.SetIndex(new object[] { keyValue });
                    probabilityData.Set<object>(FieldToSplitOn, keyValue);

                    // Add in all other numeric columns.
                    bool haveWrittenProbabilityColumn = false;

                    foreach (DataColumn column in simulationData.Columns)
                    {
                        if (column.DataType == typeof(double))
                        {
                            var values = group.Get<double>(column.ColumnName).ToList();
                            values.Sort();

                            if (!haveWrittenProbabilityColumn)
                            {
                                // Add in the probability column
                                double[] probabilityValues = MathUtilities.ProbabilityDistribution(values.Count, this.Exceedence);
                                probabilityData.SetValues("Probability", probabilityValues);
                                haveWrittenProbabilityColumn = true;
                            }

                            probabilityData.SetValues(column.ColumnName, values);
                        }
                    }
                }

                // Write the stats data to the DataStore
                DataTable t = probabilityData.ToTable();
                t.TableName = this.Name;
                dataStore.Writer.WriteTable(t);
            }
        }
    }
}
