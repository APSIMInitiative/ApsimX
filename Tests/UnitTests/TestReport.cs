namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Report;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    [TestFixture]
    public class TestReport
    {
        private static void CallEvent(IModel model, string eventName)
        {
            MethodInfo eventToInvoke = model.GetType().GetMethod("On" + eventName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (eventToInvoke != null)
                eventToInvoke.Invoke(model, new object[] { model, new EventArgs() });
        }

        private static void InjectLink(IModel model, string linkFieldName, object linkFieldValue)
        {
            ReflectionUtilities.SetValueOfFieldOrProperty(linkFieldName, model, linkFieldValue);
        }

        [Test]
        public void EnsureReportWritesToStorage()
        {
            MockLocator locator = new MockLocator();
            MockStorage storage = new MockStorage();

            Report report = new Report();
            report.VariableNames = new string[] { "A", "B", "C" };
            report.EventNames = new string[0];

            InjectLink(report, "simulation", new Simulation() { Name = "Sim1" });
            InjectLink(report, "clock", new MockClock());
            InjectLink(report, "storage", storage);
            InjectLink(report, "locator", locator);
            InjectLink(report, "events", new MockEvents());

            CallEvent(report, "Commencing");

            locator.Values["A"] = new VariableObject(10);
            locator.Values["B"] = new VariableObject(20);
            locator.Values["C"] = new VariableObject(30);
            report.DoOutput(null, null);

            locator.Values["A"] = new VariableObject(40);
            locator.Values["B"] = new VariableObject(50);
            locator.Values["C"] = new VariableObject(60);
            report.DoOutput(null, null);
            CallEvent(report, "Completed");


            Assert.AreEqual(storage.columnNames.ToArray(), new string[] { "Zone", "A", "B", "C" });
            Assert.AreEqual(storage.rows.Count, 2);
            Assert.AreEqual(storage.rows[0].values, new object[] { null, 10, 20, 30 });
            Assert.AreEqual(storage.rows[1].values, new object[] { null, 40, 50, 60 });
        }
    }
}