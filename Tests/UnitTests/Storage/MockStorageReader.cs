using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Storage;
using Moq;
using Moq.Language.Flow;

namespace UnitTests.Storage
{
    public class MockStorageReader : IStorageReader
    {
        private IEnumerable<DataTable> tables;
        private readonly Mock<IStorageReader> mockReader;

        public MockStorageReader(params DataTable[] tables)
        {
            this.tables = tables;
            List<string> checkpoints = new List<string>() { "Current" };
            mockReader = new Mock<IStorageReader>();
            mockReader.Setup(m => m.CheckpointNames).Returns(checkpoints);

            foreach (DataTable table in tables)
            {
                mockReader.Setup(m => m.ColumnNames(table.TableName)).Returns(table.GetColumnNames().ToList());
                SetupGetData(table.TableName).Returns(table);
            }
            SetupGetData(It.IsNotIn(tables.Select(t => t.TableName))).Throws<InvalidOperationException>();
        }

        private ISetup<IStorageReader, DataTable> SetupGetData(string tableName)
        {
            return mockReader.Setup(m => m.GetData(tableName, It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>()));
        }

        public List<string> CheckpointNames => mockReader.Object.CheckpointNames;

        public List<string> SimulationNames => mockReader.Object.SimulationNames;

        public List<string> TableNames => mockReader.Object.TableNames;

        public List<string> ViewNames => mockReader.Object.ViewNames;

        public List<string> TableAndViewNames => mockReader.Object.TableAndViewNames;

        public List<string> ColumnNames(string tableName) => mockReader.Object.ColumnNames(tableName);

        public void ExecuteSql(string sql)
        {
            throw new NotImplementedException();
        }

        public string FullColumnName(string tablename, string queryColumnName)
        {
            throw new NotImplementedException();
        }

        public int GetCheckpointID(string checkpointName)
        {
            throw new NotImplementedException();
        }

        public bool GetCheckpointShowOnGraphs(string checkpointName)
        {
            throw new NotImplementedException();
        }

        public List<Tuple<string, Type>> GetColumns(string tableName)
        {
            throw new NotImplementedException();
        }

        public DataTable GetData(string tableName, string checkpointName = "Current", IEnumerable<string> simulationNames = null, IEnumerable<string> fieldNames = null, string filter = null, int from = 0, int count = 0, IEnumerable<string> orderByFieldNames = null, bool distinct = false)
        {
            DataTable result = mockReader.Object.GetData(tableName, checkpointName, simulationNames, fieldNames, filter, from, count, orderByFieldNames, distinct);
            IEnumerable<string> columns = result.GetColumnNames();
            if (fieldNames.Any(f => !columns.Contains(f)))
                throw new Exception($"Column missing from table {tableName}");
            if (simulationNames != null && simulationNames.Any() && !columns.Contains("SimulationID") && !columns.Contains("SimulationName"))
                throw new InvalidOperationException($"{tableName} does not contain a simulationID column");
            return result;
        }

        public DataTable GetDataUsingSql(string sql)
        {
            throw new NotImplementedException();
        }

        public void Refresh()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> ToSimulationIDs(IEnumerable<string> simulationNames) => mockReader.Object.ToSimulationIDs(simulationNames);

        public bool TryGetSimulationID(string simulationName, out int simulationID) => mockReader.Object.TryGetSimulationID(simulationName, out simulationID);

        public string Units(string tableName, string columnHeading) => mockReader.Object.Units(tableName, columnHeading);
    }
}