using APSIM.Shared.Utilities;
using Models.PostSimulationTools;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Data;
using System.IO;
using System.Reflection;

namespace UnitTests
{
    public class ExcelInputTests
    {
        private IDatabaseConnection database;

        /// <summary>Find and return the file name of SQLite runtime .dll</summary>
        public static string FindSqlite3DLL()
        {
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] files = Directory.GetFiles(directory, "sqlite3.dll");
            if (files.Length == 1)
                return files[0];

            throw new Exception("Cannot find sqlite3 dll directory");
        }

        [Test]
        public void LoadExcelInput()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            if (ProcessUtilities.CurrentOS.IsWindows)
            {
                string sqliteSourceFileName = FindSqlite3DLL();
                Directory.SetCurrentDirectory(Path.GetDirectoryName(sqliteSourceFileName));
            }

            database = new SQLite();
            database.OpenDatabase(":memory:", readOnly: false);

            var dataStore = new DataStore(database);
            dataStore.Writer.TablesModified.Add("Observed");

            ExcelInput excelInput = new ExcelInput();
            excelInput.FileNames = new string[] {"%root%/Tests/UnitTests/PostSimulationTools/Input.xlsx"};
            excelInput.SheetNames = new string[] {"Sheet1"};

            Utilities.InjectLink(excelInput, "storage", dataStore);

            excelInput.Run();
            dataStore.Writer.Stop();
            dataStore.Reader.Refresh();
            
            DataTable dt = dataStore.Reader.GetData("Sheet1");

            Assert.That(dt.Columns[4].DataType, Is.EqualTo(typeof(DateTime)));
            Assert.That(dt.Columns[5].DataType, Is.EqualTo(typeof(string)));
            Assert.That(dt.Columns[6].DataType, Is.EqualTo(typeof(string)));
        }
    }
}
