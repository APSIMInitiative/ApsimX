using System;
using System.Data;
using System.Collections.Generic;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.IO;
using ExcelDataReader;
using System.Linq;

namespace APSIM.Shared.Utilities
{
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

        /// <summary>
        /// Reads the contents of an OpenXML Excel spreadsheet.
        /// </summary>
        /// <param name="filename">The excel file.</param>
        /// <param name="sheetname">The sheet to read.</param>
        /// <returns>DataTable representation of the excel spreadsheet.</returns>
        /// <remarks>Dates are expected to be handled by the caller.</remarks>
        public static DataTable ReadOpenXMLFileData(string filename, string sheetname)
        {
            using FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var ssd = SpreadsheetDocument.Open(fs, false);
            var wb = ssd.WorkbookPart;
            var sharedStrings = wb.SharedStringTablePart?.SharedStringTable.Elements<SharedStringItem>().Select(ssi => ssi.Text.Text).ToList() ?? [];
            var sheet = (Sheet)wb.Workbook.Sheets.FirstOrDefault(s => (s as Sheet).Name == sheetname) ?? throw new Exception($"No sheet with sheet name {sheetname}!");
            var rows = (wb.GetPartById(sheet.Id) as WorksheetPart).Worksheet.Descendants<Row>();
            DataTable ret = new();
            var colcount = rows.Select(r => CellToColumnIndex(r.Elements<Cell>().Last())).Max();
            for (int i = 0; i <= colcount; ++i)
            {
                ret.Columns.Add($"Column{i}", typeof(string));
            }
            foreach (var row in rows)
            {
                var newRow = ret.NewRow();
                var blankLine = true;
                foreach (var cell in row.Elements<Cell>())
                {
                    var txt = HandleCell(cell);
                    if (blankLine)
                        blankLine = string.IsNullOrEmpty(txt);
                    newRow[CellToColumnIndex(cell)] = txt;
                }
                if (!blankLine)
                    ret.Rows.Add(newRow);
            }
            // This step is necessary to have edits be tracked, otherwise deleting a row will throw indexing off elsewhere in APSIM.
            ret.AcceptChanges();
            return ret;

            string HandleCell(Cell cell)
            {
                if (cell.DataType == null)
                    // NOTE: Default is a number.
                    return cell?.CellValue?.Text;
                return cell.DataType.Value switch
                {
                    CellValues.SharedString => sharedStrings[Convert.ToInt32(cell.CellValue.Text)],
                    CellValues.InlineString => cell.InlineString.Text.Text,
                    _ => cell.CellValue.Text,
                };
            }

            int CellToColumnIndex(Cell cell)
            {
                // Cell references are strings like "AA1" where the letters are the column index in base26 and number is the row index.
                string cellRef = cell.CellReference.Value;
                int ret = 0;
                foreach (char ch in cellRef.ToUpper())
                {
                    if (ch < 'A')
                        break;
                    ret *= 26;
                    ret += ch - ('A' - 1);
                }
                return ret - 1;
            }
        }

        /// <summary>Returns true if extension matches an OpenXML Excel extension.</summary>
        public static bool IsOpenXMLExcelFile(string filename)
            => openXmlExtensions.Contains(Path.GetExtension(filename).ToLower());

    }
}
