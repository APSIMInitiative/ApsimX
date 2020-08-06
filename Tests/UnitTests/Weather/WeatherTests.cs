using Models;
using Models.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace UnitTests.Weather
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
            string weatherFilePath = Path.ChangeExtension(Path.GetTempFileName(), ".xlsx");
            using (FileStream file = new FileStream(weatherFilePath, FileMode.Create, FileAccess.Write))
            {
                Assembly.GetExecutingAssembly().GetManifestResourceStream("UnitTests.Weather.WeatherTestsExcelFile.xlsx").CopyTo(file);
            }

            Simulation baseSim = new Simulation()
            {
                Name = "Base",
                Children = new List<IModel>()
                {
                    new Models.Climate.Weather()
                    {
                        Name = "Weather",
                        FullFileName = weatherFilePath,
                        ExcelWorkSheetName = "Sheet1"
                    },
                    new Clock()
                    {
                        Name = "Clock",
                        StartDate = new DateTime(1998, 11, 9),
                        EndDate = new DateTime(1998, 11, 12)
                    },
                    new MockSummary()
                }
            };

            baseSim.Run();
            Assert.AreEqual(MockSummary.messages[0], "Simulation terminated normally");
        }
    }
}
