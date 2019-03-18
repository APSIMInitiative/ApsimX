namespace UnitTests
{
    using Models.Storage;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    public class ReportDataTests
    {
        private class Record1
        {
            public int a { get; set; }
            public Record2[] b { get; set; }
        }

        private class Record2
        {
            public int c { get; set; }
        }
        /// <summary>Convert block of scalar report data to a table.</summary>
        [Test]
        public void ScalarsToTable()
        {
            var data = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col1", "Col2", "Col3", "Col4" },
                ColumnUnits = new string[] {   null,   null, "g/m2",   "mm" }
            };

            data.Rows.Add(new List<object>() { new DateTime(2017, 1, 1), 1.0, 1, "abc" });
            data.Rows.Add(new List<object>() { new DateTime(2017, 1, 2), 2.0, 2, "def" });

            Assert.AreEqual(Utilities.TableToString(data.ToTable()),
            "      Col1, Col2,Col3,Col4\r\n" +
            "2017-01-01,1.000,   1, abc\r\n" +
            "2017-01-02,2.000,   2, def\r\n");
        }

        /// <summary>Convert an array to a table.</summary>
        [Test]
        public void ArraysToTable()
        {
            var data = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col1", "Col2" },
                ColumnUnits = new string[] {   "mm",    "g" }
            };

            data.Rows.Add(new List<object>() { new int[] { 1, 2    }, 10 });
            data.Rows.Add(new List<object>() { new int[] { 3, 4, 5 }, 20 });

            Assert.AreEqual(Utilities.TableToString(data.ToTable()),
            "Col1(1),Col1(2),Col2,Col1(3)\r\n" +
            "      1,      2,  10,       \r\n" +
            "      3,      4,  20,      5\r\n");
        }

        /// <summary>Convert an array of structures to a table.</summary>
        [Test]
        public void ArrayStructuresToTable()
        {
            var data = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col1" },
                ColumnUnits = new string[] { null }
            };

            var value1 = new Record1()
            {
                a = 1,
                b = new Record2[]
                {
                    new Record2() { c = 2 },
                    new Record2() { c = 3 }
                }
            };
            var value2 = new Record1()
            {
                a = 4,
                b = new Record2[]
                {
                    new Record2() { c = 5 },
                    new Record2() { c = 6 }
                }
            };


            data.Rows.Add(new List<object>() { value1 });
            data.Rows.Add(new List<object>() { value2 });

            Assert.AreEqual(Utilities.TableToString(data.ToTable()),
            "Col1.a,Col1.b(1).c,Col1.b(2).c\r\n" +
            "     1,          2,          3\r\n" +
            "     4,          5,          6\r\n");

        }
    }
}