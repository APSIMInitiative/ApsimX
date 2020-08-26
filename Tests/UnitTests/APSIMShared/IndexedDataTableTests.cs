namespace UnitTests.APSIMShared
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models;
    using NUnit.Framework;
    using System.Data;

    /// <summary>
    /// Tests for <see cref="IndexedDataTable"/>.
    /// </summary>
    [TestFixture]
    public class IndexedDataTableTests
    {
        [Test]
        public void TestSetVectorThenSetScalar()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(new string[] { "Year", "Name" });

            indexedTable.SetIndex(new object[] { 2000, "Name1" });
            indexedTable.SetValues("A", new double[] { 1, 2, 3, 4 });  // vector
            indexedTable.Set("B", 1234);  // scalar

            string expected = ReflectionUtilities.GetResourceAsString("UnitTests.APSIMShared.IndexedDataTableTests.TestSetVectorThenScalar.Expected.txt");
            Assert.AreEqual(Utilities.TableToString(indexedTable.ToTable()), expected);
        }

        [Test]
        public void TestSetScalarThenSetVector()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(new string[] { "Year", "Name" });

            indexedTable.SetIndex(new object[] { 2000, "Name1" });
            indexedTable.Set("A", 1234);  // scalar
            indexedTable.SetValues("B", new double[] { 1, 2, 3, 4 });  // vector

            string expected = ReflectionUtilities.GetResourceAsString("UnitTests.APSIMShared.TestSetScalarThenVector.Expected.txt");
            Assert.AreEqual(Utilities.TableToString(indexedTable.ToTable()), expected);
        }

        [Test]
        public void TestNoIndexSetVectorThenScalar()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(null);

            indexedTable.SetValues("A", new double[] { 1, 2, 3, 4 });  // vector
            indexedTable.Set("B", 1234);  // scalar

            string expected = ReflectionUtilities.GetResourceAsString("UnitTests.APSIMShared.TestNoIndexSetVectorThenScalar.Expected.txt");
            Assert.AreEqual(Utilities.TableToString(indexedTable.ToTable()), expected);
        }

        [Test]
        public void TestNoIndexSetScalarThenSetVector()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(null);

            indexedTable.Set("A", 1234);  // scalar
            indexedTable.SetValues("B", new double[] { 1, 2, 3, 4 });  // vector

            string expected = ReflectionUtilities.GetResourceAsString("UnitTests.APSIMShared.TestNoIndexSetScalarThenSetVector.Expected.txt");
            Assert.AreEqual(Utilities.TableToString(indexedTable.ToTable()), expected);
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
            string expected = ReflectionUtilities.GetResourceAsString("UnitTests.APSIMShared.TestChangeIndex.Expected.txt");
            Assert.AreEqual(Utilities.TableToString(indexedTable.ToTable()), expected);
        }

        [Test]
        public void TestGetColumn()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(new string[] { "Year", "Name" });

            indexedTable.SetIndex(new object[] { 2000, "Name1" });
            indexedTable.SetValues("A", new double[] { 1, 2, 3, 4 });  // vector
            indexedTable.Set("B", 1234);  // scalar

            indexedTable.SetIndex(new object[] { 2001, "Name2" });
            indexedTable.SetValues("A", new double[] { 5, 6, 7, 8 });  // vector
            indexedTable.Set("B", 5678);  // scalar

            IndexedDataTable indexedTable2 = new IndexedDataTable(indexedTable.ToTable(), new string[] { "Year" });
            indexedTable2.SetIndex(new object[] { 2000 });

            var a = indexedTable2.Get<double>("A");
            Assert.AreEqual(a, new double[] { 1, 2, 3, 4 });

            var names = indexedTable2.Get<string>("Name");
            Assert.AreEqual(names, new string[] { "Name1", "Name1", "Name1", "Name1" });

            indexedTable2.SetIndex(new object[] { 2001 });
            var b = indexedTable2.Get<int>("B");
            Assert.AreEqual(b, new int[] { 5678, 5678, 5678, 5678 });
        }

        [Test]
        public void TestIterateThroughGroups()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(new string[] { "Year", "Name" });

            indexedTable.SetIndex(new object[] { 2000, "Name1" });
            indexedTable.SetValues("A", new double[] { 1, 2, 3, 4 });  // vector
            indexedTable.Set("B", 1234);  // scalar

            indexedTable.SetIndex(new object[] { 2001, "Name2" });
            indexedTable.SetValues("A", new double[] { 5, 6, 7, 8 });  // vector
            indexedTable.Set("B", 5678);  // scalar

            int i = 1;
            foreach (var group in indexedTable.Groups())
            {
                var a = group.Get<double>("A");
                if (i == 1)
                    Assert.AreEqual(a, new double[] { 1, 2, 3, 4 });
                else
                    Assert.AreEqual(a, new double[] { 5, 6, 7, 8 });
                i++;
            }

        }

    }
}