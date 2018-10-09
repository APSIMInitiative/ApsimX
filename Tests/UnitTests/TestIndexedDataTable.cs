namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Report;
    using NUnit.Framework;
    using System.Data;

    [TestFixture]
    public class TestIndexedDataTable
    {
        [Test]
        public void TestSetVectorThenSetScalar()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(new string[] { "Year", "Name" });

            indexedTable.SetIndex(new object[] { 2000, "Name1" });
            indexedTable.SetValues("A", new double[] { 1, 2, 3, 4 });  // vector
            indexedTable.Set("B", 1234);  // scalar

            Assert.AreEqual(Utilities.TableToString(indexedTable.ToTable()),
                           "Year, Name,    A,   B\r\n" +
                           "2000,Name1,1.000,1234\r\n" +
                           "2000,Name1,2.000,1234\r\n" +
                           "2000,Name1,3.000,1234\r\n" +
                           "2000,Name1,4.000,1234\r\n");
        }

        [Test]
        public void TestSetScalarThenSetVector()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(new string[] { "Year", "Name" });

            indexedTable.SetIndex(new object[] { 2000, "Name1" });
            indexedTable.Set("A", 1234);  // scalar
            indexedTable.SetValues("B", new double[] { 1, 2, 3, 4 });  // vector

            Assert.AreEqual(Utilities.TableToString(indexedTable.ToTable()),
                           "Year, Name,   A,    B\r\n" +
                           "2000,Name1,1234,1.000\r\n" +
                           "2000,Name1,1234,2.000\r\n" +
                           "2000,Name1,1234,3.000\r\n" +
                           "2000,Name1,1234,4.000\r\n");
        }

        [Test]
        public void TestNoIndexSetVectorThenScalar()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(null);

            indexedTable.SetValues("A", new double[] { 1, 2, 3, 4 });  // vector
            indexedTable.Set("B", 1234);  // scalar

            Assert.AreEqual(Utilities.TableToString(indexedTable.ToTable()),
                           "    A,   B\r\n" +
                           "1.000,1234\r\n" +
                           "2.000,1234\r\n" +
                           "3.000,1234\r\n" +
                           "4.000,1234\r\n");
        }

        [Test]
        public void TestNoIndexSetScalarThenSetVector()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(null);

            indexedTable.Set("A", 1234);  // scalar
            indexedTable.SetValues("B", new double[] { 1, 2, 3, 4 });  // vector

            Assert.AreEqual(Utilities.TableToString(indexedTable.ToTable()),
                           "   A,    B\r\n" +
                           "1234,1.000\r\n" +
                           "1234,2.000\r\n" +
                           "1234,3.000\r\n" +
                           "1234,4.000\r\n");
        }

        [Test]
        public void TestChangeIndex()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(new string[] { "Year", "Name" });

            indexedTable.SetIndex(new object[] { 2000, "Name1" });
            indexedTable.SetValues("A", new double[] { 1, 2, 3, 4 });  // vector
            indexedTable.Set("B", 1234);  // scalar

            indexedTable.SetIndex(new object[] { 2001, "Name2" });
            indexedTable.SetValues("A", new double[] { 5, 6, 7, 8 });  // vector
            indexedTable.Set("B", 5678);  // scalar

            Assert.AreEqual(Utilities.TableToString(indexedTable.ToTable()),
                           "Year, Name,    A,   B\r\n" +
                           "2000,Name1,1.000,1234\r\n" +
                           "2000,Name1,2.000,1234\r\n" +
                           "2000,Name1,3.000,1234\r\n" +
                           "2000,Name1,4.000,1234\r\n" + 
                           "2001,Name2,5.000,5678\r\n" +
                           "2001,Name2,6.000,5678\r\n" +
                           "2001,Name2,7.000,5678\r\n" +
                           "2001,Name2,8.000,5678\r\n");
        }

    }
}