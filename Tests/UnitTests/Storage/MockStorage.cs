using Models.Core;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.Data;

namespace UnitTests.Storage
{
    [Serializable]
    internal class MockStorage : Model, IDataStore, IStorageWriter, IStorageReader
    {
        internal List<string> columnNames = new List<string>();
        internal List<Row> rows = new List<Row>();

        internal List<DataTable> tables = new List<DataTable>();

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

        public IStorageReader Reader { get { return this; } }

        public IStorageWriter Writer { get { return this; } }

        public List<string> CheckpointNames => throw new NotImplementedException();

        List<string> IStorageReader.SimulationNames => throw new NotImplementedException();

        List<string> IStorageReader.TableNames => throw new NotImplementedException();

        public List<string> ViewNames => throw new NotImplementedException();

        public List<string> TableAndViewNames => throw new NotImplementedException();

        [Serializable]
        internal class Row
        {
            public IList<object> values;
        }

        public DataTable GetData(string tableName, string checkpointName = null, string simulationName = null, IEnumerable<string> fieldNames = null, string filter = null, int from = 0, int count = 0, string groupBy = null)
        {
            return null;
        }

        public T[] Get<T>(string columnName)
        {
            int columnIndex = columnNames.IndexOf(columnName);
            if (columnIndex != -1)
            {
                var values = new T[rows.Count];
                for (int i = 0; i != rows.Count; i++)
                    values[i] = (T) rows[i].values[columnIndex];
                return values;
            }
            return null;
        }

        public string GetUnits(string tableName, string columnHeading)
        {
            return null;
        }

        public void WriteTable(DataTable table)
        {
            tables.Add(table);
        }

        public void DeleteDataInTable(string tableName)
        {
        }

        public DataTable RunQuery(string sql)
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

        public int GetSimulationID(string simulationName, string folderName)
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
                rows.Add(new Row() { values = APSIM.Shared.Utilities.ReflectionUtilities.Clone(dataRow) as IList<object> });

        }

        public void AddUnits(string tableName, IEnumerable<string> columnNames, IEnumerable<string> columnUnits)
        {
            throw new NotImplementedException();
        }

        public void Open()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public DataTable GetData(string tableName, string checkpointName = null, string simulationName = null, IEnumerable<string> fieldNames = null, string filter = null, int from = 0, int count = 0, string orderBy = null, bool distinct = false)
        {
            throw new NotImplementedException();
        }

        public DataTable GetDataUsingSql(string sql)
        {
            throw new NotImplementedException();
        }

        public string Units(string tableName, string columnHeading)
        {
            throw new NotImplementedException();
        }

        public List<string> ColumnNames(string tableName)
        {
            throw new NotImplementedException();
        }

        public List<string> StringColumnNames(string tableName)
        {
            throw new NotImplementedException();
        }

        public string BriefColumnName(string tablename, string fullColumnName)
        {
            throw new NotImplementedException();
        }

        public string FullColumnName(string tablename, string queryColumnName)
        {
            throw new NotImplementedException();
        }

        public int GetSimulationID(string simulationName)
        {
            throw new NotImplementedException();
        }

        public void Refresh()
        {
        }

        public void AddView(string name, string selectSQL)
        {
            throw new NotImplementedException();
        }

        public List<Tuple<string, Type>> GetColumns(string tableName)
        {
            throw new NotImplementedException();
        }

        public void DeleteTable(string tableName)
        {
            throw new NotImplementedException();
        }

        public void SetCheckpointShowGraphs(string name, bool showGraphs)
        {
            throw new NotImplementedException();
        }

        public bool GetCheckpointShowOnGraphs(string checkpointName)
        {
            throw new NotImplementedException();
        }
    }
}