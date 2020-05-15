// An APSIMInputFile is either a ".met" file or a ".out" file.
// They are both text files that share the same format. 
// These classes are used to read/write these files and create an object instance of them.


namespace APSIM.Shared.Utilities
{
    using System;
    using System.Data;
    using System.Collections.Generic;
    using DocumentFormat.OpenXml.Packaging;
    using DocumentFormat.OpenXml.Spreadsheet;
    using System.IO;
    using ExcelDataReader;
    
    
    /// <summary>
    /// Utilities for working with Excel (".xlxs") Files
    /// </summary>
    public class ExcelUtilities
    {
        #region Variables and Constants 

        /// <summary>
        /// a constant to represent a valid excel file extension.
        /// </summary>
        public const string ExcelExtension = ".xlsx";

        #endregion


        /// <summary>
        /// This will read the names of all of the worksheets within this file
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>A list of the Sheet Names in the specified file (as strings).</returns>
        public static List<string> GetWorkSheetNames(string fileName)
        {
            List<string> names = new List<string>();
            using (SpreadsheetDocument document = SpreadsheetDocument.Open(fileName, false))
            {
                Sheets allSheets = document.WorkbookPart.Workbook.Sheets;
                foreach (Sheet sheet in allSheets)
                {
                    names.Add(sheet.Name);
                }
            }
            return names;
        }

        /// <summary>
        /// Opens and reads an excel (".xlsx") file and returns the data from the specified sheet
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="sheetName"></param>
        /// <param name="headerRow"></param>
        /// <returns>a DataTable</returns>
        public static DataTable ReadExcelFileData(string fileName, string sheetName, bool headerRow = false)
        {
            DataTable data = new DataTable();

            if (Path.GetExtension(fileName) != ExcelUtilities.ExcelExtension)
            {
                throw new Exception("The Excel File must be an '.xlsx' file.");
            }

            using (FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                //Reading from a binary Excel file ('97-2003 format; *.xls)
                //IExcelDataReader excelReader = ExcelReaderFactory.CreateBinaryReader(stream);

                //Reading from a OpenXml Excel file (2007 format; *.xlsx)
                IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);

                DataSet result;
                if (headerRow)
                    // Read all sheets from the EXCEL file as a data set
                    // excelReader.IsFirstRowAsColumnNames = true;
                    result = excelReader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });
                else
                    result = excelReader.AsDataSet();
                data = result.Tables[sheetName];

                return data;
            }
        }
    }
}
