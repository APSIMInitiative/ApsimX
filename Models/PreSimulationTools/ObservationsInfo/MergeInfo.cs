using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using APSIM.Numerics;
using APSIM.Shared.Utilities;

namespace Models.PreSimulationTools.ObservationsInfo
{
    /// <summary>
    /// 
    /// </summary>
    public class MergeInfo
    {
        /// <summary></summary>
        public string Name;

        /// <summary></summary>
        public string Date;

        /// <summary></summary>
        public string Column;

        /// <summary></summary>
        public string Value1;

        /// <summary></summary>
        public string Value2;

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
            newTable.Columns.Add("Date");
            newTable.Columns.Add("Column");
            newTable.Columns.Add("Value1");
            newTable.Columns.Add("Value2");
            newTable.Columns.Add("File");

            if (data == null)
                return newTable;

            foreach (MergeInfo info in data)
            {
                DataRow row = newTable.NewRow();
                row["Name"] = info.Name;
                row["Date"] = info.Date;
                row["Column"] = info.Column;
                row["Value1"] = info.Value1;
                row["Value2"] = info.Value2;
                row["File"] = info.File;
                newTable.Rows.Add(row);
            }

            for (int i = 0; i < newTable.Columns.Count; i++)
                newTable.Columns[i].ReadOnly = true;

            DataView dv = newTable.DefaultView;
            dv.Sort = "Name asc";

            return dv.ToTable();
        }
        
        /// <summary>
        /// Filter through the given datatable and combine rows that have the same SimulationName and Clock.Today values.
        /// Will throw if it encounters two rows with different values for a field that should be combined.
        /// </summary>
        public static List<MergeInfo> CombineRows(DataTable dataTable, out DataTable combinedDataTable)
        {
            List<MergeInfo> infos = new List<MergeInfo>();

            if (!dataTable.GetColumnNames().ToList().Contains("Clock.Today"))
            {
                combinedDataTable = dataTable;
                return infos;
            }

            //Get a distinct list of rows of SimulationName and Clock.Today
            var distinctRows = dataTable.AsEnumerable()
                .Select(s => new
                {
                    simulation = s["SimulationName"],
                    clock = s["Clock.Today"],
                    clockAsString = s["Clock.Today"].ToString()
                })
                .Distinct();

            combinedDataTable = dataTable.Clone();

            string errors = "";
            foreach (var item in distinctRows)
            {
                if (!string.IsNullOrEmpty(item.clockAsString))
                {
                    //select all rows in original datatable with this distinct values
                    IEnumerable<DataRow> results = dataTable.Select().Where(p => p["SimulationName"] == item.simulation && p["Clock.Today"].ToString() == item.clockAsString);

                    //store the list of columns in the datatable
                    List<string> columns = dataTable.GetColumnNames().ToList<string>();

                    //the one or more rows needed to capture the data during merging
                    //multiple lines may still be needed if there are conflicts in columns when trying to merge the data
                    List<DataRow> newRows = new List<DataRow>();

                    foreach (DataRow row in results)
                    {
                        foreach (string column in columns)
                        {
                            if (!string.IsNullOrEmpty(row[column].ToString()))
                            {
                                bool merged = false;
                                foreach (DataRow newRow in newRows)
                                {
                                    if (!merged)
                                    {
                                        if (CanMergeRows(row, newRow, column) < 0.25)
                                        {
                                            newRow[column] = row[column];
                                            merged = true;
                                        }
                                        else
                                        {
                                            MergeInfo info = new MergeInfo();
                                            info.Name = item.simulation.ToString();
                                            info.Date = Convert.ToDateTime(item.clock).ToString("dd/MM/yyyy");
                                            info.Column = column;
                                            info.Value1 = newRow[column].ToString();
                                            info.Value2 = row[column].ToString();
                                            info.File = newRow["_Filename"].ToString();
                                            infos.Add(info);
                                        }
                                    }
                                }
                                if (!merged)
                                {
                                    DataRow duplicateRow = combinedDataTable.NewRow();
                                    duplicateRow["SimulationName"] = item.simulation;
                                    duplicateRow["Clock.Today"] = item.clock;
                                    duplicateRow[column] = row[column];
                                    newRows.Add(duplicateRow);
                                }
                            }
                        }
                    }

                    //add the rows to the result dataTable
                    foreach (DataRow dupilcateRow in newRows)
                        combinedDataTable.Rows.Add(dupilcateRow);
                }
            }

            if (!string.IsNullOrEmpty(errors))
                throw new System.Exception(errors);

            return infos;
        }

        /// <summary>
        /// Comparisions to check if the value in the given column can be merged into newRow from row.
        /// </summary>
        /// <returns>Returns the percentage difference between values, 0 if equal, 1 if string mismatch, 0-1 for double mismatch.</returns>
        private static double CanMergeRows(DataRow row, DataRow newRow, string column)
        {
            if (!string.IsNullOrEmpty(row[column].ToString()))
            {
                if (!Observations.RESERVED_COLUMNS.Contains(column) && !string.IsNullOrEmpty(newRow[column].ToString()))
                {
                    bool isDouble1 = double.TryParse(newRow[column].ToString(), out double existing);
                    bool isDouble2 = double.TryParse(row[column].ToString(), out double other);
                    if (isDouble1 && isDouble2)
                    {
                        if (!MathUtilities.FloatsAreEqual(existing, other))
                        {
                            double percent = existing / other;
                            if (existing > other)
                                percent = other / existing;
                            return 1 - percent;
                        }
                        else
                            return 0;
                    }
                    else if (newRow[column].ToString() != row[column].ToString())
                    {
                        return 1;
                    }

                }
            }
            return 0;
        }
    }

}
