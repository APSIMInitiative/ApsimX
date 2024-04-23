using ClosedXML.Excel;
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
