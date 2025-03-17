namespace UnitTests.APSIMShared
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models;
    using NUnit.Framework;
    using System.Data;
    using System.Collections.Generic;

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

            Assert.That(
                    Utilities.CreateTable(new string[]                      { "Year",  "Name", "A",   "B" },
                                          new List<object[]> { new object[] {   2000, "Name1",   1, 1234 },
                                                               new object[] {   2000, "Name1",   2, 1234 },
                                                               new object[] {   2000, "Name1",   3, 1234 },
                                                               new object[] {   2000, "Name1",   4, 1234 } })
                   .IsSame(indexedTable.ToTable()), Is.True);
        }

        [Test]
        public void TestSetScalarThenSetVector()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(new string[] { "Year", "Name" });

            indexedTable.SetIndex(new object[] { 2000, "Name1" });
            indexedTable.Set("A", 1234);  // scalar
            indexedTable.SetValues("B", new double[] { 1, 2, 3, 4 });  // vector

            Assert.That(
                    Utilities.CreateTable(new string[]                      { "Year",  "Name",  "A", "B" },
                                          new List<object[]> { new object[] {   2000, "Name1", 1234, 1 },
                                                               new object[] {   2000, "Name1", 1234, 2 },
                                                               new object[] {   2000, "Name1", 1234, 3 },
                                                               new object[] {   2000, "Name1", 1234, 4 } })
                   .IsSame(indexedTable.ToTable()), Is.True);
        }

        [Test]
        public void TestNoIndexSetVectorThenScalar()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(null);

            indexedTable.SetValues("A", new double[] { 1, 2, 3, 4 });  // vector
            indexedTable.Set("B", 1234);  // scalar

            Assert.That(
                    Utilities.CreateTable(new string[]                      {   "A",   "B" },
                                          new List<object[]> { new object[] {     1, 1234 },
                                                               new object[] {     2, 1234 },
                                                               new object[] {     3, 1234 },
                                                               new object[] {     4, 1234 } })
                   .IsSame(indexedTable.ToTable()), Is.True);
        }

        [Test]
        public void TestNoIndexSetScalarThenSetVector()
        {
            IndexedDataTable indexedTable = new IndexedDataTable(null);

            indexedTable.Set("A", 1234);  // scalar
            indexedTable.SetValues("B", new double[] { 1, 2, 3, 4 });  // vector

            Assert.That(
                    Utilities.CreateTable(new string[]                      {  "A", "B" },
                                          new List<object[]> { new object[] { 1234,  1 },
                                                               new object[] { 1234,  2 },
                                                               new object[] { 1234,  3 },
                                                               new object[] { 1234,  4 } })
                   .IsSame(indexedTable.ToTable()), Is.True);
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


            Assert.That(
                    Utilities.CreateTable(new string[]                      { "Year", "Name", "A",   "B" },
                                          new List<object[]> { new object[] {   2000, "Name1",   1, 1234 },
                                                               new object[] {   2000, "Name1",   2, 1234 },
                                                               new object[] {   2000, "Name1",   3, 1234 },
                                                               new object[] {   2000, "Name1",   4, 1234 },
                                                               new object[] {   2001, "Name2",   5, 5678 },
                                                               new object[] {   2001, "Name2",   6, 5678 },
                                                               new object[] {   2001, "Name2",   7, 5678 },
                                                               new object[] {   2001, "Name2",   8, 5678 } })
                   .IsSame(indexedTable.ToTable()), Is.True);
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
            Assert.That(a, Is.EqualTo(new double[] { 1, 2, 3, 4 }));

            var names = indexedTable2.Get<string>("Name");
            Assert.That(names, Is.EqualTo(new string[] { "Name1", "Name1", "Name1", "Name1" }));

            indexedTable2.SetIndex(new object[] { 2001 });
            var b = indexedTable2.Get<int>("B");
            Assert.That(b, Is.EqualTo(new int[] { 5678, 5678, 5678, 5678 }));
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
                    Assert.That(a, Is.EqualTo(new double[] { 1, 2, 3, 4 }));
                else
                    Assert.That(a, Is.EqualTo(new double[] { 5, 6, 7, 8 }));
                i++;
            }

        }

    }
}