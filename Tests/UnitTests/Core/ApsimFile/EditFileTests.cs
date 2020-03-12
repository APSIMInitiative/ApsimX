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

            // Add a report.
            Models.Report report = new Models.Report();
            report.Name = "Report";
            Structure.Add(report, paddock);
            
            basicFile.Write(basicFile.FileName);
            fileName = basicFile.FileName;
        }

        [Test]
        public void TestEditing()
        {
            string configFile = Path.GetTempFileName();
            File.WriteAllLines(configFile, new[]
            {
                // Modify an array
                "[Report].VariableNames = x, y, z",

                // Modify a string
                "[Weather].FileName = fdsa.met"
            });

            string models = typeof(IModel).Assembly.Location;
            string args = $"{fileName} /Edit {configFile}";
            
            var proc = new ProcessUtilities.ProcessWithRedirectedOutput();
            proc.Start(models, args, Path.GetTempPath(), true, true);
            proc.WaitForExit();

            Simulations file = FileFormat.ReadFromFile<Simulations>(fileName, out List<Exception> errors);
            if (errors != null && errors.Count > 0)
                throw errors[0];

            var report = Apsim.Find(file, typeof(Models.Report)) as Models.Report;
            Assert.AreEqual(3, report.VariableNames.Length);

            var weather = Apsim.Find(file, typeof(Models.Weather)) as Models.Weather;
            Assert.AreEqual("fdsa.met", weather.FileName);
        }
    }
}
