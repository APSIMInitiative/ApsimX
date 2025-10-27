using NUnit.Framework;
using System.Data;
using System.IO;
using System.Linq;
using Utility;
using APSIM.Shared.Utilities;

namespace UnitTests.UtilityTests
{
    [TestFixture]
    public class DataUtilitiesTests
    {
        [Test]
        public void ReadFromEXCELTest()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            string directory = PathUtilities.GetAbsolutePath("%root%", null);
            string filename = directory + "/Tests/UnitTests/UtilityTests/data.xlsx";
            DataTable dt = Excel.ReadFromEXCEL(filename).FirstOrDefault();

            Assert.That(string.IsNullOrEmpty(dt.Rows[0]["Value"].ToString()));
            Assert.That(((double)dt.Rows[1]["Value"]).Equals(1));
            Assert.That(((double)dt.Rows[2]["Value"]).Equals(2));
        }

        [Test]
        public void WriteToEXCELTest()
        {
            string filename = Path.GetTempFileName() + ".xlsx";

            DataTable table = new DataTable("Test");
            table.Columns.Add(new DataColumn("SimulationName", typeof(string)));
            table.Columns.Add(new DataColumn("Value", typeof(int)));
            // Add rows to the DataTable
            for (int i = 0; i < 5; i++)
            {
                DataRow row = table.NewRow();
                row["SimulationName"] = "MySimulation";
                row["Value"] = i;
                table.Rows.Add(row);
            }

            Excel.WriteToEXCEL([table], filename);

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            DataTable dt = Excel.ReadFromEXCEL(filename).FirstOrDefault();
            for (int i = 0; i < 5; i++)
                Assert.That(((double)dt.Rows[i]["Value"]).Equals(i));
        }

        [Test]
        public void WriteToEXCELTestNullTests()
        {
            string filename = Path.GetTempFileName() + ".xlsx";

            DataTable table = new DataTable("Test");
            table.Columns.Add(new DataColumn("SimulationName", typeof(string)));
            table.Columns.Add(new DataColumn("Value", typeof(double)));

            DataRow row;

            row = table.NewRow();
            row["SimulationName"] = "MySimulation";
            row["Value"] = 0;
            table.Rows.Add(row);

            row = table.NewRow();
            row["SimulationName"] = "MySimulation";
            row["Value"] = double.NaN;
            table.Rows.Add(row);

            row = table.NewRow();
            row["SimulationName"] = "MySimulation";
            row["Value"] = double.PositiveInfinity;
            table.Rows.Add(row);

            row = table.NewRow();
            row["SimulationName"] = "MySimulation";
            row["Value"] = double.NegativeInfinity;
            table.Rows.Add(row);

            Excel.WriteToEXCEL([table], filename);

            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            DataTable dt = Excel.ReadFromEXCEL(filename).FirstOrDefault();
            Assert.That(((double)dt.Rows[0]["Value"]).Equals(0));
            Assert.That(string.IsNullOrEmpty(dt.Rows[1]["Value"].ToString()));
            Assert.That(string.IsNullOrEmpty(dt.Rows[2]["Value"].ToString()));
            Assert.That(string.IsNullOrEmpty(dt.Rows[3]["Value"].ToString()));
        }
    }
}
