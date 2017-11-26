namespace UnitTests
{
    using Models.Core;
    using Models.Report;
    using NUnit.Framework;

    [TestFixture]
    public class TestReport
    {
        [Test]
        public void EnsureReportWritesToStorage()
        {
            MockLocator locator = new MockLocator();
            MockStorage storage = new MockStorage();

            Report report = new Report();
            report.VariableNames = new string[] { "A", "B", "C" };
            report.EventNames = new string[0];

            Utilities.InjectLink(report, "simulation", new Simulation() { Name = "Sim1" });
            Utilities.InjectLink(report, "clock", new MockClock());
            Utilities.InjectLink(report, "storage", storage);
            Utilities.InjectLink(report, "locator", locator);
            Utilities.InjectLink(report, "events", new MockEvents());

            Utilities.CallEvent(report, "Commencing");

            locator.Values["A"] = new VariableObject(10);
            locator.Values["B"] = new VariableObject(20);
            locator.Values["C"] = new VariableObject(30);
            report.DoOutput();

            locator.Values["A"] = new VariableObject(40);
            locator.Values["B"] = new VariableObject(50);
            locator.Values["C"] = new VariableObject(60);
            report.DoOutput();
            Utilities.CallEvent(report, "Completed");


            Assert.AreEqual(storage.columnNames.ToArray(), new string[] { "Zone", "A", "B", "C" });
            Assert.AreEqual(storage.rows.Count, 2);
            Assert.AreEqual(storage.rows[0].values, new object[] { null, 10, 20, 30 });
            Assert.AreEqual(storage.rows[1].values, new object[] { null, 40, 50, 60 });
        }
    }
}