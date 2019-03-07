using Models.Core;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Data;

namespace UnitTests
{
    [Serializable]
    internal class MockStorage : Model, IDataStore, IStorageWriter
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

        string IDataStore.FileName { get; }

        public IStorageReader Reader => throw new NotImplementedException();

        public IStorageWriter Writer { get { return this; } }

        [Serializable]
        internal class Row
        {
            public IEnumerable<object> values;
        }

        public DataTable GetData(string tableName, string checkpointName = null, string simulationName = null, IEnumerable<string> fieldNames = null, string filter = null, int from = 0, int count = 0, string groupBy = null)
        {
            return null;
        }

        public string GetUnits(string tableName, string columnHeading)
        {
            return null;
        }

        public void WriteTable(DataTable table)
        {
        }

        public void DeleteDataInTable(string tableName)
        {
        }

        public DataTable RunQuery(string sql)
        {
            return null;
        }

        public IEnumerable<string> ColumnNames(string tableName)
        {
            return null;
        }

        public void DeleteAllTables(bool cleanSlate = false)
        {
        }

        public void BeginWriting(IEnumerable<string> knownSimulationNames = null, IEnumerable<string> simulationNamesBeingRun = null)
        {
        }

        public void EndWriting()
        {
        }

        public void EmptyDataStore()
        {
        }

        public int GetSimulationID(string simulationName)
        {
            return 0;
        }

        public void AllCompleted()
        {
        }

        public void CompletedWritingSimulationData(string simulationName)
        {
        }

        public void WriteTableRaw(DataTable data)
        {
        }

        public List<string> Checkpoints()
        {
            throw new NotImplementedException();
        }

        public int GetCheckpointID(string checkpointName)
        {
            throw new NotImplementedException();
        }

        public void AddUnitsForTable(string tableName, List<string> columnNames, List<string> columnUnits)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get a list of the table columns
        /// </summary>
        /// <param name="tableName">The table name</param>
        /// <returns></returns>
        public List<string> GetTableColumns(string tableName)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Empty()
        {
            throw new NotImplementedException();
        }

        public void AddCheckpoint(string name, IEnumerable<string> filesToStore = null)
        {
            throw new NotImplementedException();
        }

        public void DeleteCheckpoint(string name)
        {
            throw new NotImplementedException();
        }

        public void RevertCheckpoint(string name)
        {
            throw new NotImplementedException();
        }

        public void WaitForIdle()
        {
        }

        public void Stop()
        {
        }

        public void WriteTable(ReportData data)
        {
            this.columnNames.Clear();
            this.columnNames.AddRange(data.ColumnNames);
            foreach (var dataRow in data.Rows)
                rows.Add(new Row() { values = APSIM.Shared.Utilities.ReflectionUtilities.Clone(dataRow) as IEnumerable<object> });

        }

        public void AddUnits(string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits)
        {
            throw new NotImplementedException();
        }
    }
}