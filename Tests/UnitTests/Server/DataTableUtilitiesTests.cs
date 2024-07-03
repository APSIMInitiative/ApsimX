using NUnit.Framework;
using System;
using APSIM.Shared.Utilities;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace UnitTests.Server
{
    [TestFixture]
    public class DataTableUtilitiesTests
    {
        [Test]
        public void TestMergeTables()
        {
            DataTable table1 = new DataTable("Table1");
            table1.Columns.Add("x", typeof(double));
            table1.Columns.Add("y", typeof(double));
            table1.Rows.Add(0, 1);
            table1.Rows.Add(1, 2);
            table1.Rows.Add(2, 4);

            DataTable table2 = new DataTable("Table2");
            table2.Columns.Add("x", typeof(double));
            table2.Columns.Add("y", typeof(double));
            table2.Rows.Add(0, 1);
            table2.Rows.Add(1, 3);
            table2.Rows.Add(2, 9);

            TestMerge(table1, table2);
        }

        [Test]
        public void TestMergeNull()
        {
            Assert.That(DataTableUtilities.Merge(null), Is.Null);
        }

        [Test]
        public void TestMergeNothing()
        {
            DataTable result = DataTableUtilities.Merge(Enumerable.Empty<DataTable>());
            Assert.That(result.TableName, Is.EqualTo(""));
            Assert.That(result.Rows.Count, Is.EqualTo(0));
            Assert.That(result.Columns.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// Merge a collection of tables and ensure that the resultant table
        /// contains all rows from all of the tables.
        /// </summary>
        /// <param name="tables">Tables to be merged.</param>
        private void TestMerge(params DataTable[] tables)
        {
            // Merge the tables.
            DataTable merged = DataTableUtilities.Merge(tables);

            // Ensure that merge worked correctly.
            string expectedName = tables.FirstOrDefault()?.TableName ?? "";
            Assert.That(merged.TableName, Is.EqualTo(expectedName));

            int i = 0;
            foreach (DataTable table in tables)
            {
                foreach (DataRow row in table.Rows)
                {
                    for (int j = 0; j < table.Columns.Count; j++)
                        Assert.That(merged.Rows[i][j], Is.EqualTo(row[j]));
                    i++;
                }
            }
        }
    }
}
