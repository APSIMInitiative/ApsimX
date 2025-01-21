using APSIM.Shared.Utilities;
using Models.Core;
using NUnit.Framework;
using Models.Core.ApsimFile;
using Models.Core.Run;
using System.Collections.Generic;
using Models.Storage;
using System.Globalization;
using UnitTests.Weather;
using System.Data;

namespace UnitTests.SurfaceOrganicMatterTests
{

    /// <summary>
    /// Unit Tests for SurfaceOrganicMatterTests
    /// </summary>
    class SurfaceOrganicMatterTests
    {
        /// <summary>
        /// This test checks that the tilled event is being invoked
        /// </summary>
        [Test]
        public void SurfaceOrganicMatterTilledEvent()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Surface.SurfaceOrganicMatterEventsCheck.apsimx");
            Simulations file = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

            // This simulation needs a weather node, but using a legit
            // met component will just slow down the test.
            IModel sim = file.FindInScope<Simulation>();
            Model weather = new MockWeather();
            sim.Children.Add(weather);
            weather.Parent = sim;

            // Run the file.
            var Runner = new Runner(file);
            Runner.Run();

            // Check that the report reported on the correct dates.
            var storage = file.FindInScope<IDataStore>();
            List<string> fieldNames = new List<string>() { "doy", "lyingwt" };

            DataTable data = storage.Reader.GetData("ReportOnTilled", fieldNames: fieldNames);
            double[] values = DataTableUtilities.GetColumnAsDoubles(data, "doy", CultureInfo.InvariantCulture);
            double[] expected = new double[] { 1, 2, 3, 4 };

            double[] valuesN = DataTableUtilities.GetColumnAsDoubles(data, "lyingwt", CultureInfo.InvariantCulture);
            double[] expectedN = new double[] { 500, 250, 125, 62.5 };

            Assert.That(values, Is.EqualTo(expected));
            Assert.That(valuesN, Is.EqualTo(expectedN));
        }
    }
}
