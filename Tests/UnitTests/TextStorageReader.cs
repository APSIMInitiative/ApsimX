
namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;

    class TextStorageReader : IStorageReader
    {
        private DataTable data = new DataTable();
        private List<string> headings = new List<string>();
        private List<string> units = new List<string>();

        /// <summary>Constructor.</summary>
        /// <param name="csvData">The data to read.</param>
        public TextStorageReader(string csvData)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvData)))
            {
                var apsimReader = new ApsimTextFile();
                apsimReader.Open(stream);
                data = apsimReader.ToTable();
                apsimReader.Close();
                foreach (var unit in apsimReader.Units)
                    units.Add(unit);
                foreach (var heading in apsimReader.Headings)
                    headings.Add(heading);

            }
        }

        public List<string> CheckpointNames { get { return DataTableUtilities.GetColumnAsStrings(data, "CheckpointName").Distinct().ToList(); } }

        public List<string> SimulationNames { get { return DataTableUtilities.GetColumnAsStrings(data, "SimulationName").Distinct().ToList(); } }

        public List<string> TableNames { get { return new List<string>() { "Report" }; } }

        public List<string> ColumnNames(string tableName) { return DataTableUtilities.GetColumnNames(data).ToList(); }

        public int GetCheckpointID(string checkpointName) { return 1; }

        public int GetSimulationID(string simulationName) { return 1; }

        public string Units(string tableName, string columnHeading)
        {
            int index = headings.IndexOf(columnHeading);
            if (index == -1)
                return null;
            else
                return units[index];
        }

        public DataTable GetDataUsingSql(string sql) { throw new System.NotImplementedException(); }

        public DataTable GetData(string tableName, string checkpointName = null, string simulationName = null, IEnumerable<string> fieldNames = null, string filter = null, int from = 0, int count = 0, string orderBy = null)
        {
            string rowFilter = null;
            if (checkpointName != null)
                rowFilter += "CheckpointName = '" + checkpointName + "'";
            if (simulationName != null)
            {
                if (rowFilter != null) rowFilter += " AND ";
                rowFilter += "SimulationName = '" + simulationName + "'";
            }
            if (filter != null)
            {
                if (rowFilter != null) rowFilter += " AND ";
                rowFilter += "(" + filter + ")";
            }

            if (rowFilter == null)
                return data;
            else if (fieldNames == null)
            {
                var view = new DataView(data);
                view.RowFilter = rowFilter;
                return view.ToTable();
            }

            else
            {
                var dataCopy = data.Copy();
                foreach (DataColumn column in data.Columns)
                {
                    if (!fieldNames.Contains(column.ColumnName) &&
                        column.ColumnName != "CheckpointName")
                        dataCopy.Columns.Remove(column.ColumnName);
                }

                var view = new DataView(dataCopy);
                view.RowFilter = rowFilter;
                return view.ToTable();
            }
        }

        public List<string> StringColumnNames(string tableName)
        {
            throw new System.NotImplementedException();
        }
    }

}
