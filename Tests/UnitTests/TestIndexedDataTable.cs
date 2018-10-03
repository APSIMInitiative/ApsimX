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
        public void EnsureReportWritesToStorage()
        {
            DataTable table = new DataTable();
            IndexedDataTable indexedTable = new IndexedDataTable(table, new string[] { "Year", "Name" });

            indexedTable.SetIndex(new object[] { 2000, "Name1" });
            indexedTable.Set("A", 1234);
        }
    }
}