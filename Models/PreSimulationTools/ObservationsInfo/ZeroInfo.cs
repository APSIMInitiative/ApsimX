using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using APSIM.Numerics;
using APSIM.Shared.Utilities;

namespace Models.PreSimulationTools.ObservationsInfo
{
    /// <summary>
    /// Stores information about a rows that contain 0 values that might need to be cleaned up
    /// </summary>
    public class ZeroInfo
    {
        /// <summary>The simulation name</summary>
        public string Name;

        /// <summary>The Clock.Today of the row with a 0</summary>
        public string Date;

        /// <summary>The column where the 0 is</summary>
        public string Column;

        /// <summary>The file containing this 0</summary>
        public string File;

        /// <summary>
        /// Converts a list of ZeroInfo into a DataTable
        /// </summary>
        /// <param name="data">A list of ZeroInfo</param>
        /// <returns>A DataTable of the contents of the given list. Used by the GUI for displaying this class.</returns>
        public static DataTable CreateDataTable(IEnumerable data)
        {
            DataTable newTable = new DataTable();

            newTable.Columns.Add("Name");
            newTable.Columns.Add("Date");
            newTable.Columns.Add("Column");
            newTable.Columns.Add("File");

            if (data == null)
                return newTable;

            foreach (ZeroInfo info in data)
            {
                DataRow row = newTable.NewRow();
                row["Name"] = info.Name;
                row["Date"] = info.Date;
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
        /// Looks through the given DataTable for any cells that equal "0"
        /// </summary>
        /// <param name="dataTable">The DataTable to look at</param>
        /// <returns>A list of ZeroInfo that help track down the found 0s</returns>
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
                            info.Date = Convert.ToDateTime(row["Clock.Today"].ToString()).ToString("dd/MM/yyyy");
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
