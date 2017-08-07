using System;
using System.Collections.Generic;
using Models.Report;
using Models.Core;
using System.Data;

namespace UnitTests
{
    internal class MockStorage : IStorage
    {
        internal List<string> columnNames = new List<string>();
        internal List<Row> rows = new List<Row>();

        public string[] SimulationNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string FileName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public IEnumerable<string> TableNames
        {
            get
            {
                throw new NotImplementedException();
            }
        }

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

        public DataTable GetData(string tableName, string simulationName = null, IEnumerable<string> fieldNames = null, string filter = null, int from = 0, int count = 0)
        {
            throw new NotImplementedException();
        }

        public string GetUnits(string tableName, string columnHeading)
        {
            throw new NotImplementedException();
        }

        public void WriteTable(string tableName, DataTable table)
        {
            throw new NotImplementedException();
        }

        public void DeleteTable(string tableName)
        {
            throw new NotImplementedException();
        }

        public DataTable RunQuery(string sql)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> ColumnNames(string tableName)
        {
            throw new NotImplementedException();
        }

        public void StoreFactors(string experimentName, string simulationName, string folderName, List<string> names, List<string> values)
        {
            throw new NotImplementedException();
        }

        public void DeleteAllTables(bool cleanSlate = false)
        {
            throw new NotImplementedException();
        }
    }
}