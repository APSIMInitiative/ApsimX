using ClosedXML.Excel;
using System;
using System.Data;

namespace Utility
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class Excel
    {
        /// <summary>
        /// Write all outputs to an EXCEL file
        /// </summary>
        /// <param name="tables">The array of tables to write</param>
        /// <param name="fileName">The file name to write to</param>
        public static void WriteToEXCEL(System.Data.DataTable[] tables, string fileName)
        {
            XLWorkbook workbook = new XLWorkbook();
            foreach (System.Data.DataTable table in tables)
            {
                DataTable sortedTable = table;

                //set any infinity or NaN values to DBNull so that closedXML can write them
                foreach (DataColumn column in table.Columns)
                {
                    string columnName = column.ColumnName;
                    if (column.DataType == typeof(double) || column.DataType == typeof(object))
                        foreach (DataRow row in table.Rows)
                            if ((double)row[columnName] == double.NaN || (double)row[columnName] == double.NegativeInfinity || (double)row[columnName] == double.PositiveInfinity)
                                row[column.ColumnName] = DBNull.Value;
                }

                //Sort Rows by SimulationNatableme in alphabetical order
                if (table.Columns.Contains("SimulationName"))
                {
                    DataView dv = table.DefaultView;
                    dv.Sort = "SimulationName ASC";
                    if (table.Columns.Contains("Clock.Today"))
                        dv.Sort += ", Clock.Today ASC";
                    sortedTable = dv.ToTable();
                }
                workbook.Worksheets.Add(sortedTable);
            }

            workbook.SaveAs(fileName);
        }
    }
}
