using Models.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models;
using Models.Core.ApsimFile;
using APSIM.Shared.Utilities;

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

        private string extFile;

        [SetUp]
        public void Initialise()
        {
            Simulations basicFile = Utilities.GetRunnableSim();

            IModel simulation = Apsim.Find(basicFile, typeof(Simulation));
            IModel paddock = Apsim.Find(basicFile, typeof(Zone));

            // Add a weather component.
            Models.Weather weather = new Models.Weather();
            weather.FileName = "asdf.met";
            Structure.Add(weather, simulation);

            // Add a second weather component.
            Models.Weather weather2 = new Models.Weather();
            weather2.FileName = "asdf.met";
            weather2.Name = "Weather2";
            Structure.Add(weather2, simulation);

            // Add a third weather component.
            Models.Weather weather3 = new Models.Weather();
            weather3.FileName = "asdf.met";
            weather3.Name = "Weather3";
            Structure.Add(weather3, simulation);

            // Add a third weather component.
            Models.Weather weather4 = new Models.Weather();
            weather4.FileName = "asdf.met";
            weather4.Name = "Weather4";
            Structure.Add(weather4, simulation);

            // Add a report.
            Models.Report report = new Models.Report();
            report.Name = "Report";
            Structure.Add(report, paddock);
            
            basicFile.Write(basicFile.FileName);
            fileName = basicFile.FileName;

            // Create a new .apsimx file containing two weather nodes.
            Simulations test = Utilities.GetRunnableSim();
            IModel sim = Apsim.Find(test, typeof(Simulation));

            Models.Weather w1 = new Models.Weather();
            w1.FileName = "w1.met";
            w1.Name = "w1";
            Structure.Add(w1, sim);

            Models.Weather w2 = new Models.Weather();
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
                @"[Weather2].FullFileName = .\jkl.met",

                // Replace a model with a model from another file.
                $"[Weather3] = {extFile}",
                $"[Weather4] = {extFile};[w2]",
            });

            string models = typeof(IModel).Assembly.Location;
            string args = $"{fileName} /Edit {configFile}";
            
            var proc = new ProcessUtilities.ProcessWithRedirectedOutput();
            proc.Start(models, args, Path.GetTempPath(), true, true);
            proc.WaitForExit();

            Assert.AreEqual(null, proc.StdOut);
            Assert.AreEqual(null, proc.StdErr);

            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName, out List<Exception> errors);
            if (errors != null && errors.Count > 0)
                throw errors[0];

            var report = Apsim.Find(file, typeof(Models.Report)) as Models.Report;
            string[] variableNames = new[] { "x", "y", "z" };
            Assert.AreEqual(variableNames, report.VariableNames);

            Clock clock = Apsim.Find(file, typeof(Clock)) as Clock;
            Assert.AreEqual(new DateTime(2000, 1, 1), clock.StartDate);
            Assert.AreEqual(new DateTime(2000, 1, 10), clock.EndDate);

            var weather = Apsim.Find(file, "Weather") as Models.Weather;
            Assert.AreEqual("fdsa.met", weather.FileName);

            var weather2 = Apsim.Find(file, "Weather2") as Models.Weather;
            Assert.AreEqual(@".\jkl.met", weather2.FileName);

            // Weather3 and Weather4 should have been renamed to w1 and w2, respectively.
            var weather3 = Apsim.Find(file, "w1") as Models.Weather;
            Assert.AreEqual("w1", weather3.Name);
            Assert.AreEqual("w1.met", weather3.FileName);

            var weather4 = Apsim.Find(file, "w2") as Models.Weather;
            Assert.AreEqual("w2", weather4.Name);
            Assert.AreEqual("w2.met", weather4.FileName);
        }
    }
}
