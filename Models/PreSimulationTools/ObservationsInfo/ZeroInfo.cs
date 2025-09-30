using System.Collections;
using System.Collections.Generic;
using System.Data;
using APSIM.Numerics;
using APSIM.Shared.Utilities;

namespace Models.PreSimulationTools.ObservationsInfo
{
    /// <summary>
    /// 
    /// </summary>
    public class ZeroInfo
    {
        /// <summary></summary>
        public string Name;

        /// <summary></summary>
        public string Column;

        /// <summary></summary>
        public string File;

        /// <summary>
        /// Converts a list of SimulationInfo into a DataTable
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static DataTable CreateDataTable(IEnumerable data)
        {
            DataTable newTable = new DataTable();

            newTable.Columns.Add("Name");
            newTable.Columns.Add("Column");
            newTable.Columns.Add("File");

            if (data == null)
                return newTable;

            foreach (ZeroInfo info in data)
            {
                DataRow row = newTable.NewRow();
                row["Name"] = info.Name;
                row["Column"] = info.Column;
                row["File"] = info.File;
                newTable.Rows.Add(row);
            }

            for (int i = 0; i < newTable.Columns.Count; i++)
                newTable.Columns[i].ReadOnly = true;

            DataView dv = newTable.DefaultView;
            dv.Sort = "Column asc";

            return dv.ToTable();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<ZeroInfo> DetectZeros(DataTable dataTable)
        {
            List<ZeroInfo> data = new List<ZeroInfo>();
            foreach (string column in dataTable.GetColumnNames())
            {
                if (!column.EndsWith("Error"))
                {
                    foreach (DataRow row in dataTable.Rows)
                    {
                        string value = row[column].ToString();
                        bool isDouble = double.TryParse(value, out double number);
                        if (isDouble && MathUtilities.FloatsAreEqual(0, number))
                        {
                            ZeroInfo info = new ZeroInfo();
                            info.Name = row["SimulationName"].ToString();
                            info.Column = column;
                            info.File = row["_Filename"].ToString();
                            data.Add(info);
                        }
                    }
                }
            }
            return data;
        }
    }
}
