using Models.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using Models.Climate;
using Models.Core.ApsimFile;
using APSIM.Shared.Utilities;
using Models.PMF;
using Models.Functions;
using Models.Soils;
using Models.WaterModel;
using Models.Surface;

namespace UnitTests.Core.ApsimFile
{
    /// <summary>
    /// A test set for the edit file feature which
    /// allows for automated editing of .apsimx
    /// files from the command line by using the
    /// /Edit switch on Models.exe.
    /// </summary>
    [TestFixture]
    public class EditFileTests
    {
        /// <summary>
        /// Path to the .apsimx file.
        /// </summary>
        private string fileName;

        /// <summary>
        /// Path to a second .apsimx file which has a few weather
        /// nodes in it, which will be imported into the first
        /// .apsimx file via the /Edit feature.
        /// </summary>
        private string extFile;

        [SetUp]
        public void Initialise()
        {
            Simulations basicFile = Utilities.GetRunnableSim();

            IModel simulation = basicFile.FindInScope<Simulation>();
            IModel paddock = basicFile.FindInScope<Zone>();

            // Add a weather component.
            Models.Climate.Weather weather = new Models.Climate.Weather();
            weather.Name = "Weather";
            weather.FileName = "asdf.met";
            Structure.Add(weather, simulation);

            // Add a second weather component.
            Models.Climate.Weather weather2 = new Models.Climate.Weather();
            weather2.FileName = "asdf.met";
            weather2.Name = "Weather2";
            Structure.Add(weather2, simulation);

            // Add a third weather component.
            Models.Climate.Weather weather3 = new Models.Climate.Weather();
            weather3.FileName = "asdf.met";
            weather3.Name = "Weather3";
            Structure.Add(weather3, simulation);

            // Add a third weather component.
            Models.Climate.Weather weather4 = new Models.Climate.Weather();
            weather4.FileName = "asdf.met";
            weather4.Name = "Weather4";
            Structure.Add(weather4, simulation);

            // Add a report.
            Models.Report report = new Models.Report();
            report.Name = "Report";
            Structure.Add(report, paddock);

            // Add the wheat model.
            string json = ReflectionUtilities.GetResourceAsString(typeof(IModel).Assembly, "Models.Resources.Wheat.json");
            Plant wheat = FileFormat.ReadFromString<IModel>(json, out _).Children[0] as Plant;
            wheat.ResourceName = "Wheat";
            Structure.Add(wheat, paddock);

            Manager manager = new Manager();
            manager.Code = @"using Models.PMF;
using Models.Core;
using System;
namespace Models
{
    [Serializable]
    public class Script : Model
    {
        [Description(""an amount"")]
        public double Amount { get; set; }
    }
}";
            Structure.Add(manager, paddock);

            Physical physical = new Physical();
            physical.BD = new double[5];
            physical.AirDry = new double[5];
            physical.LL15 = new double[5];
            Structure.Add(physical, paddock);
            Structure.Add(new WaterBalance(), paddock);
            Structure.Add(new SurfaceOrganicMatter(), paddock);

            basicFile.Write(basicFile.FileName);
            fileName = basicFile.FileName;

            // Create a new .apsimx file containing two weather nodes.
            Simulations test = Utilities.GetRunnableSim();
            IModel sim = test.FindInScope<Simulation>();

            Models.Climate.Weather w1 = new Models.Climate.Weather();
            w1.FileName = "w1.met";
            w1.Name = "w1";
            Structure.Add(w1, sim);

            Models.Climate.Weather w2 = new Models.Climate.Weather();
            w2.Name = "w2";
            w2.FileName = "w2.met";
            Structure.Add(w2, sim);

            extFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".apsimx");
            test.Write(extFile);
        }

        [Test]
        public void TestEditing()
        {

            string configFile = Path.GetTempFileName();
            File.WriteAllLines(configFile, new[]
            {
                // Modify an array
                "[Report].VariableNames = x,y,z",

                // Modify a date - try a few different formats.
                "[Clock].StartDate = 2000-01-01",
                "[Clock].EndDate = 2000-01-10T00:00:00",

                // Modify a string
                "[Weather].FileName = fdsa.met",
                @"[Weather2].FullFileName = jkl.met",

                // Replace a model with a model from another file.
                $"[Weather3] = {extFile}",
                $"[Weather4] = {extFile};[w2]",

                // Change a property of a resource model.
                "[Wheat].Leaf.Photosynthesis.RUE.FixedValue = 0.4",

                // Change a property of a manager script.
                "[Manager].Script.Amount = 1234",

                // Set an entire array.
                "[Physical].BD = 1, 2, 3, 4, 5",
                
                // Modify a single element of an array.
                "[Physical].AirDry[2] = 6",

                // Modify multiple elements of an array.
                "[Physical].LL15[3:4] = 7",
            });

            Simulations file = EditFile.Do(fileName, configFile);

            var report = file.FindInScope<Models.Report>();
            string[] variableNames = new[] { "x", "y", "z" };
            Assert.AreEqual(variableNames, report.VariableNames);

            IModel sim = file.FindChild<Simulation>();

            // Use an index-based lookup to locate child models.
            // When we replace an entire model, we want to ensure
            // that the replacement is inserted at the correct index.

            Clock clock = sim.Children[0] as Clock;
            Assert.AreEqual(new DateTime(2000, 1, 1), clock.StartDate);
            Assert.AreEqual(new DateTime(2000, 1, 10), clock.EndDate);

            var weather = sim.Children[3] as Models.Climate.Weather;
            Assert.NotNull(weather);
            Assert.AreEqual("Weather", weather.Name);
            Assert.AreEqual("fdsa.met", weather.FileName);

            var weather2 = sim.Children[4] as Models.Climate.Weather;
            Assert.NotNull(weather2);
            Assert.AreEqual("Weather2", weather2.Name);
            Assert.AreEqual(@"jkl.met", weather2.FileName);

            // Weather3 and Weather4 should have been
            // renamed to w1 and w2, respectively.
            var weather3 = sim.Children[5] as Models.Climate.Weather;
            Assert.NotNull(weather3);
            Assert.AreEqual("w1", weather3.Name);
            Assert.AreEqual("w1.met", weather3.FileName);

            var weather4 = sim.Children[6] as Models.Climate.Weather;
            Assert.NotNull(weather4);
            Assert.AreEqual("w2", weather4.Name);
            Assert.AreEqual("w2.met", weather4.FileName);

            // The edit file operation should have changed RUE value to 0.4.
            var wheat = sim.Children[2].Children[2] as Plant;
            var rue = wheat.Children[6].Children[4].Children[0] as Constant;
            Assert.AreEqual(0.4, rue.FixedValue);

            double amount = (double)sim.FindByPath("[Manager].Script.Amount")?.Value;
            Assert.AreEqual(1234, amount);

            Physical physical = sim.Children[2].Children[4] as Physical;
            Assert.AreEqual(new double[5] { 1, 2, 3, 4, 5 }, physical.BD);
            Assert.AreEqual(new double[5] { 0, 6, 0, 0, 0 }, physical.AirDry);
            Assert.AreEqual(new double[5] { 0, 0, 7, 7, 0 }, physical.LL15);
        }
    }
}
