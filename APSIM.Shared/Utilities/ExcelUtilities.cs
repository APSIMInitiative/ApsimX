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
    using System.Linq;


    /// <summary>
    /// Utilities for working with Excel (".xlxs") Files
    /// </summary>
    public class ExcelUtilities
    {
        /// <summary>
        /// List of common binary (office '03) excel file extensions.
        /// </summary>
        private static readonly string[] oldExcelFormats = new string[]
        {
            ".xls"
        };

        /// <summary>
        /// List of common OpenXML (office '07) excel file extensions.
        /// </summary>
        private static readonly string[] openXmlExtensions = new string[]
        {
            ".xlsx",
            ".xlsm"
        };

        /// <summary>
        /// Checks whether a file is an Excel file.
        /// The check is purely based on file extension, so it's a bit primitive.
        /// </summary>
        /// <param name="fileName">Path/name of the file to check.</param>
        public static bool IsExcelFile(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLower();
            return openXmlExtensions.Contains(extension) || oldExcelFormats.Contains(extension);
        }

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
            using (FileStream stream = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                string fileExtension = Path.GetExtension(fileName);
                IExcelDataReader excelReader;
                if (oldExcelFormats.Contains(fileExtension))
                    // Reading from a binary Excel file ('97-2003 format; *.xls)
                    excelReader = ExcelReaderFactory.CreateBinaryReader(stream);
                else
                    // Assume that any unknown extension is an OpenXML file.
                    // The call to CreateOpenXmlReader should throw if this is untrue.
                    //
                    // Reading from a OpenXml Excel file (2007 format; *.xlsx)
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                var worksheets = GetWorkSheetNames(fileName);
                var sheetIndex = worksheets.FindIndex((s) => s == sheetName);
                if (sheetIndex < 0)
                    return null;
                ExcelDataSetConfiguration cfg = new() { FilterSheet = (_, ind) => ind == sheetIndex };
                if (headerRow)
                    cfg.ConfigureDataTable = (_) => new() { UseHeaderRow = true };

                return excelReader.AsDataSet(cfg).Tables[sheetName];
            }
        }
    }
}
