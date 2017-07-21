using System;
using System.Collections.Generic;
using Models.Report;
using Models.Core;

namespace UnitTests
{
    internal class MockStorage : IStorage
    {
        internal List<string> columnNames = new List<string>();
        internal List<Row> rows = new List<Row>();
        internal class Row
        {
            public IEnumerable<object> values;
        }

        /// <summary>Write to permanent storage.</summary>
        /// <param name="simulationName">Name of simulation</param>
        /// <param name="tableName">Name of table</param>
        /// <param name="columnNames">Column names</param>
        /// <param name="columnUnits">Column units</param>
        /// <param name="valuesToWrite">Values of row to write</param>
        public void WriteRow(string simulationName, string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits, IEnumerable<object> valuesToWrite)
        {
            this.columnNames.Clear();
            this.columnNames.AddRange(columnNames);
            rows.Add(new Row() { values = APSIM.Shared.Utilities.ReflectionUtilities.Clone(valuesToWrite) as IEnumerable<object> });
        }
    }
}