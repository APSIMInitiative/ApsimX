using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using ClosedXML.Excel;
using ExcelDataReader;

namespace Utility
{
    /// <summary>
    /// A class for writing a collection of DataTables to an excel file, where each table is a different sheet.
    /// </summary>
    public class Excel
    {
        /// <summary>
        /// Write all outputs to an EXCEL file
        /// </summary>
        /// <param name="tables">The array of tables to write</param>
        /// <param name="fileName">The file name to write to</param>
        public static void WriteToEXCEL(DataTable[] tables, string fileName)
        {
            XLWorkbook workbook = new XLWorkbook();
            foreach (DataTable table in tables)
            {
                DataTable sortedTable = table;

                //set any infinity or NaN values to DBNull so that closedXML can write them
                foreach (DataColumn column in table.Columns)
                {
                    if (column.DataType == typeof(double) || column.DataType == typeof(object))
                    {
                        foreach (DataRow row in table.Rows)
                        {
                            string columnName = column.ColumnName;
                            var value = row[columnName];
                            if (value != DBNull.Value && value is double valueDouble)
                                if (double.IsNaN(valueDouble) || valueDouble == double.NegativeInfinity || valueDouble == double.PositiveInfinity)
                                    row[columnName] = DBNull.Value;
                        }
                    }
                }

                //Sort Rows by SimulationName in alphabetical order
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

        /// <summary>
        /// Read an EXCEL file to List of DataTables
        /// </summary>
        /// <param name="fileName">The file name to read</param>
        public static List<DataTable> ReadFromEXCEL(string fileName)
        {
            List<DataTable> output = new List<DataTable>();
            // Open the file
            using (FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Reading from a OpenXml Excel file (2007 format; *.xlsx)
                using (IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream))
                {
                    // Read all sheets from the EXCEL file as a data set.
                    DataSet dataSet = excelReader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        UseColumnDataType = true,
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });

                    // Write all sheets that are specified in 'SheetNames' to the data store
                    foreach (DataTable table in dataSet.Tables)
                    {
                        output.Add(table);
                    }
                }
            }
            return output;
        }
    }
}
