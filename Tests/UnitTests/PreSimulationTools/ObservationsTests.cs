using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.PostSimulationTools;
using Models.PreSimulationTools;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Data;
using System.IO;
using System.Reflection;

namespace UnitTests
{
    public class ObservationsTests
    {
        private IDatabaseConnection database;

        [Test]
        public void LoadExcelFile()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            database = new SQLite();
            database.OpenDatabase(":memory:", readOnly: false);

            var dataStore = new DataStore(database);
            dataStore.Writer.TablesModified.Add("Observed");

            Observations observations = new Observations()
            {
                FileNames = ["%root%/Tests/UnitTests/PostSimulationTools/Input.xlsx"],
                SheetNames = ["Sheet1"]
            };
            Node.Create(observations);

            Utilities.InjectLink(observations, "storage", dataStore);

            observations.Run();
            dataStore.Writer.Stop();
            dataStore.Reader.Refresh();

            DataTable dt = dataStore.Reader.GetData("Sheet1");

            Assert.That(dt.Columns[4].DataType, Is.EqualTo(typeof(DateTime)));
            Assert.That(dt.Columns[5].DataType, Is.EqualTo(typeof(string)));
            Assert.That(dt.Columns[6].DataType, Is.EqualTo(typeof(string)));

            database.CloseDatabase();
        }
    }
}
