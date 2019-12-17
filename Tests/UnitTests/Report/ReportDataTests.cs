namespace UnitTests.Report
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

        /// <summary>Convert whole arrays to a table with correct headings.</summary>
        [Test]
        public void WholeArraysToTable()
        {
            var data = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col", "Zones(1).WaterUptake" },
                ColumnUnits = new string[] {  "mm",    "g" }
            };

            data.Rows.Add(new List<object>() { new int[] { 1, 2    }, new double[] { 3.0, 4.0 } });
            data.Rows.Add(new List<object>() { new int[] { 5, 6, 7 }, new double[] { 8.0, 9.0 } });

            Assert.AreEqual(Utilities.TableToString(data.ToTable()),
            "Col(1),Col(2),Zones(1).WaterUptake(1),Zones(1).WaterUptake(2),Col(3)\r\n" +
            "     1,     2,                  3.000,                  4.000,      \r\n" +
            "     5,     6,                  8.000,                  9.000,     7\r\n");
        }

        /// <summary>Convert arrays that are initially null but later have values.</summary>
        [Test]
        public void ArraysInitiallyNullToTable()
        {
            var data = new ReportData()
            {
                CheckpointName = "Current",
                SimulationName = "Sim1",
                TableName = "Report",
                ColumnNames = new string[] { "Col" },
                ColumnUnits = new string[] { "mm" }
            };

            data.Rows.Add(new List<object>() { null });
            data.Rows.Add(new List<object>() { new int[] { 1, 2, 3 } });

            Assert.AreEqual(Utilities.TableToString(data.ToTable()),
            "Col(1),Col(2),Col(3)\r\n" +
            "      ,      ,      \r\n" +
            "     1,     2,     3\r\n");
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