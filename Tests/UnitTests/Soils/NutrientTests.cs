using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Storage;
using NUnit.Framework;

namespace UnitTests.SurfaceOrganicMatterTests
{

    /// <summary>
    /// Unit Tests for SurfaceOrganicMatterTests
    /// </summary>
    class NutrientTests
    {
        /// <summary>Ensure total carbon on the first day of the simulation isn't zero.</summary>
        [Test]
        public void EnsureTotalCOnStartOfFirstDayIsntZero()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.totalc.apsimx");

            Simulations file = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;
            Models.Climate.Weather weather = file.FindDescendant<Models.Climate.Weather>();
            string properWeatherFilePath = PathUtilities.GetRelativePath(weather.FullFileName, null);
            weather.FullFileName = properWeatherFilePath;


            // Run the file.
            var Runner = new Runner(file);
            Runner.Run();

            // Get the output data table.
            var storage = file.FindInScope<IDataStore>();
            List<string> fieldNames = new() { "sum(Nutrient.TotalC)" };
            DataTable data = storage.Reader.GetData("Report", fieldNames: fieldNames);

            // Ensure the first value isn't zero.
            double[] values = DataTableUtilities.GetColumnAsDoubles(data, fieldNames[0], CultureInfo.InvariantCulture);
            Assert.That(values.First(), Is.Not.Zero);
        }
    }
}
