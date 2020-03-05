namespace Utility
{
    using ClosedXML.Excel;

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
                workbook.Worksheets.Add(table);
            }

            workbook.SaveAs(fileName);
        }
    }
}
