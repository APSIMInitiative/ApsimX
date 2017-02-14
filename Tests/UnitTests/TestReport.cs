namespace UnitTests
{
    using Models.Report;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class TestReport
    {
        private class Pair
        {
            public int a { get; set; }
            public int b { get; set; }
        }

        private class Record
        {
            public DateTime date { get; set; }
            public List<double> values { get; set; }
            public Pair[] pairs { get; set; }
        }

        [Test]
        public void TestFlatten()
        {
            Record[] records = new Record[5];
            records[0] = new Record()
            {
                date = new DateTime(2017, 1, 1),
                values = new List<double> { 100, 101 },
                pairs = new Pair[] { new Pair() { a = 10, b = 11 },
                                     new Pair() { a = 12, b = 13 },
                                     new Pair() { a = 14, b = 15 } }
            };
            records[1] = new Record()
            {
                date = new DateTime(2017, 1, 2),
                values = new List<double> { 102, 103 },
                pairs = new Pair[] { new Pair() { a = 16, b = 17 },
                                     new Pair() { a = 18, b = 19 },
                                     new Pair() { a = 20, b = 21 },
                                     new Pair() { a = 22, b = 23 }}
            };

            records[2] = new Record()
            {
                date = new DateTime(2017, 1, 1),
                values = new List<double> { 200, 201 },
                pairs = new Pair[] { new Pair() { a = 20, b = 21 },
                                     new Pair() { a = 22, b = 23 },
                                     new Pair() { a = 24, b = 25 },
                                     new Pair() { a = 26, b = 27 },
                                     new Pair() { a = 28, b = 29 },
                                     new Pair() { a = 30, b = 31 } }
            };
            records[3] = new Record()
            {
                date = new DateTime(2017, 1, 2),
                values = new List<double> { 202, 203, 204 },
                pairs = new Pair[] { new Pair() { a = 32, b = 33 },
                                     new Pair() { a = 34, b = 35 },
                                     new Pair() { a = 36, b = 37 },
                                     new Pair() { a = 38, b = 39 }}
            };

            records[4] = new Record()
            {
                date = new DateTime(2017, 1, 3),
                values = new List<double> { 302, 303 },
                pairs = new Pair[] { new Pair() { a = 40, b = 41 },
                                     new Pair() { a = 42, b = 43 },
                                     new Pair() { a = 44, b = 45 },
                                     new Pair() { a = 46, b = 47 }}
            };

            ReportTable table = new ReportTable() { SimulationName = "Sim1", TableName = "table1" };
            table.Columns.Add(new ReportColumnWithValues("Col1", new object[] { 1, 2, 3, 4, 5 }));
            table.Columns.Add(new ReportColumnWithValues("Col2", records));
            table.Columns.Add(new ReportColumnWithValues("Col3", new object[] { 6, 7, 8, 9, 10 }));

            table.Flatten();

            Assert.AreEqual(table.Columns.Count, 18);
            Assert.AreEqual(table.Columns[0].Name, "Col1");
            Assert.AreEqual(table.Columns[1].Name, "Col2.date");
            Assert.AreEqual(table.Columns[2].Name, "Col2.pairs(1).a");
            Assert.AreEqual(table.Columns[3].Name, "Col2.pairs(1).b");
            Assert.AreEqual(table.Columns[4].Name, "Col2.pairs(2).a");
            Assert.AreEqual(table.Columns[5].Name, "Col2.pairs(2).b");
            Assert.AreEqual(table.Columns[6].Name, "Col2.pairs(3).a");
            Assert.AreEqual(table.Columns[7].Name, "Col2.pairs(3).b");
            Assert.AreEqual(table.Columns[8].Name, "Col2.pairs(4).a");
            Assert.AreEqual(table.Columns[9].Name, "Col2.pairs(4).b");
            Assert.AreEqual(table.Columns[10].Name, "Col2.pairs(5).a");
            Assert.AreEqual(table.Columns[11].Name, "Col2.pairs(5).b");
            Assert.AreEqual(table.Columns[12].Name, "Col2.pairs(6).a");
            Assert.AreEqual(table.Columns[13].Name, "Col2.pairs(6).b");
            Assert.AreEqual(table.Columns[14].Name, "Col2.values(1)");
            Assert.AreEqual(table.Columns[15].Name, "Col2.values(2)");
            Assert.AreEqual(table.Columns[16].Name, "Col2.values(3)");
            Assert.AreEqual(table.Columns[17].Name, "Col3");

            Assert.AreEqual(table.Columns[0].Values, new double[] { 1, 2, 3, 4, 5 });
            Assert.AreEqual(table.Columns[1].Values, new DateTime[] { new DateTime(2017, 1, 1),
                                                                      new DateTime(2017, 1, 2),
                                                                      new DateTime(2017, 1, 1),
                                                                      new DateTime(2017, 1, 2),
                                                                      new DateTime(2017, 1, 3)});
            Assert.AreEqual(table.Columns[2].Values,  new double[] {   10,   16,   20,   32,   40 });     // Col2.pairs(1).a
            Assert.AreEqual(table.Columns[3].Values,  new double[] {   11,   17,   21,   33,   41 });     // Col2.pairs(1).b
            Assert.AreEqual(table.Columns[4].Values,  new double[] {   12,   18,   22,   34,   42 });     // Col2.pairs(2).a
            Assert.AreEqual(table.Columns[5].Values,  new double[] {   13,   19,   23,   35,   43 });     // Col2.pairs(2).b
            Assert.AreEqual(table.Columns[6].Values,  new double[] {   14,   20,   24,   36,   44 });     // Col2.pairs(3).a
            Assert.AreEqual(table.Columns[7].Values,  new double[] {   15,   21,   25,   37,   45 });     // Col2.pairs(3).b
            Assert.AreEqual(table.Columns[8].Values,  new object[] { null,   22,   26,   38,   46 });     // Col2.pairs(4).a
            Assert.AreEqual(table.Columns[9].Values,  new object[] { null,   23,   27,   39,   47 });     // Col2.pairs(4).b
            Assert.AreEqual(table.Columns[10].Values, new object[] { null, null,   28, null, null });     // Col2.pairs(5).a
            Assert.AreEqual(table.Columns[11].Values, new object[] { null, null,   29, null, null });     // Col2.pairs(5).b
            Assert.AreEqual(table.Columns[12].Values, new object[] { null, null,   30, null, null });     // Col2.pairs(6).a
            Assert.AreEqual(table.Columns[13].Values, new object[] { null, null,   31, null, null });     // Col2.pairs(6).b
            Assert.AreEqual(table.Columns[14].Values, new object[] {  100,  102,  200,  202,  302 });     // Col2.values(1)
            Assert.AreEqual(table.Columns[15].Values, new object[] {  101,  103,  201,  203,  303 });     // Col2.values(2)
            Assert.AreEqual(table.Columns[16].Values, new object[] { null, null, null,  204, null });     // Col2.values(3)
            Assert.AreEqual(table.Columns[17].Values, new object[] {    6,    7,    8,    9,   10 });     // Col3
        }
    }
}
