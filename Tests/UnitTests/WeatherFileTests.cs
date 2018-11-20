using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.Runners;
using NUnit.Framework;
using System.Reflection;
using System.IO;

namespace UnitTests
{
    /// <summary>
    /// Tests for weather files.
    /// </summary>
    class WeatherFileTests
    {
        /// <summary>
        /// Tests a simple weather file in .xlsx (Excel) format.
        /// </summary>
        [Test]
        public void ExcelWeatherFileTest()
        {
            Simulation baseSim = new Simulation();
            baseSim.Name = "Base";

            string weatherFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".xlsx");
            using (FileStream file = new FileStream(weatherFilePath, FileMode.Create, FileAccess.Write))
            {
                Assembly.GetExecutingAssembly().GetManifestResourceStream("UnitTests.Resources.WeatherFile.xlsx").CopyTo(file);
            }

            Weather excelWeather = new Weather()
            {
                Name = "Weather",
                Parent = baseSim,
                FullFileName = weatherFilePath,
                ExcelWorkSheetName = "Sheet1"
            };

            Clock clock = new Clock()
            {
                Name = "Clock",
                Parent = baseSim,
                StartDate = new DateTime(1998, 11, 9),
                EndDate = new DateTime(1998, 11, 12)
            };

            MockSummary summary = new MockSummary()
            {
                Name = "Summary",
                Parent = baseSim
            };

            baseSim.Children = new List<Model>() { excelWeather, clock, summary };
            MockStorage storage = new MockStorage();
            Simulations simsToRun = Simulations.Create(new List<IModel> { baseSim, storage });

            IJobManager jobManager = Runner.ForSimulations(simsToRun, simsToRun, false);
            IJobRunner jobRunner = new JobRunnerSync();
            jobRunner.JobCompleted += Utilities.EnsureJobRanGreen;
            jobRunner.Run(jobManager, true);
        }
    }
}
