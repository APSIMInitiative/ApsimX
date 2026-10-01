using APSIM.Shared.Utilities;
using Models.Core;
using NUnit.Framework;
using Models.Core.Run;
using System.Collections.Generic;
using Models.Storage;
using System.Globalization;
using UnitTests.Weather;
using System.Data;
using APSIM.Core;
using Models.Surface;
using System;

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
            Simulations file = FileFormat.ReadFromString<Simulations>(json).Model as Simulations;

            // This simulation needs a weather node, but using a legit
            // met component will just slow down the test.
            IModel sim = file.Node.Find<Simulation>();
            Model weather = new MockWeather();
            sim.Children.Add(weather);
            weather.Parent = sim;

            // Run the file.
            var Runner = new Runner(file);
            Runner.Run();

            // Check that the report reported on the correct dates.
            var storage = file.Node.Find<IDataStore>();
            List<string> fieldNames = new List<string>() { "doy", "lyingwt" };

            DataTable data = storage.Reader.GetData("ReportOnTilled", fieldNames: fieldNames);
            double[] values = DataTableUtilities.GetColumnAsDoubles(data, "doy", CultureInfo.InvariantCulture);
            double[] expected = new double[] { 1, 2, 3, 4 };

            double[] valuesN = DataTableUtilities.GetColumnAsDoubles(data, "lyingwt", CultureInfo.InvariantCulture);
            double[] expectedN = new double[] { 500, 250, 125, 62.5 };

            Assert.That(values, Is.EqualTo(expected));
            Assert.That(valuesN, Is.EqualTo(expectedN));
        }

        /// <summary>
        /// Ensures ReadParam() rejects InitialStandingFraction outside the [0, 1] range.
        /// </summary>
        [TestCase(-0.01)]
        [TestCase(1.01)]
        public void SurfaceOrganicMatterInitialStandingFractionOutOfRangeThrows(double standingFraction)
        {
            var surfaceOrganicMatter = Utilities.GetModelFromResource<SurfaceOrganicMatter>("SurfaceOrganicMatter");
            Utilities.ResolveLinks(surfaceOrganicMatter);

            surfaceOrganicMatter.InitialResidueName = "wheat_stubble";
            surfaceOrganicMatter.InitialResidueType = "wheat";
            surfaceOrganicMatter.InitialResidueMass = 500;
            surfaceOrganicMatter.InitialCPR = 0;
            surfaceOrganicMatter.InitialCNR = 100;
            surfaceOrganicMatter.InitialStandingFraction = standingFraction;

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => surfaceOrganicMatter.Reset());

            Assert.That(exception.ParamName, Is.EqualTo(nameof(surfaceOrganicMatter.InitialStandingFraction)));
            Assert.That(exception.Message, Does.Contain("between 0 and 1"));
        }
    }
}
