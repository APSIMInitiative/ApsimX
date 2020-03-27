namespace UnitTests.Weather
{
    using Models;
    using Models.Core;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    /// <summary>Tests for weather randomiser</summary>
    class WeatherSamplerTests
    {
        /// <summary>Tests a 3 year weather selection.</summary>
        [Test]
        public void ThreeYearTest()
        {
            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            const double delta = 0.000001;

            var weatherFilePath = Path.GetFullPath(Path.Combine(binDirectory, "..", "Examples", "WeatherFiles", "Dalby.met"));

            var baseSim = new Simulation()
            {
                Children = new List<Model>()
                {
                    new Clock() { },
                    new WeatherSampler()
                    {
                        FileName = weatherFilePath,
                        StartDateOfSimulation = "3000-jun-01",
                        Years = new double[] { 1995, 1952, 1993 }
                    },
                    new MockSummary()
                }
            };

            var weather = baseSim.Children[1] as WeatherSampler;
            Utilities.CallEvent(weather, "StartOfSimulation");

            Assert.AreEqual(weather.StartDate, new DateTime(3000, 6, 1));
            Assert.AreEqual(weather.EndDate, new DateTime(3003, 5, 31));

            // ################## YEAR 1 - year before a leap year
            // 1st Jun 1995 in weather file
            // year day  radn  maxt   mint  rain  pan    vp    
            // 1995 152   14.0  22.5  10.0   4.3   2.2  15.0
            Utilities.CallEvent(weather, "DoWeather");
            Assert.AreEqual(weather.Radn, 14, delta);
            Assert.AreEqual(weather.MaxT, 22.5, delta);
            Assert.AreEqual(weather.MinT, 10.0, delta);
            Assert.AreEqual(weather.Rain, 4.3, delta);
            Assert.AreEqual(weather.VP, 15.0, delta);

            // Advance 364 days
            for (int i = 0; i < 365; i++)
                Utilities.CallEvent(weather, "DoWeather");

            // 31st May 1996 in weather file
            // year day  radn  maxt   mint  rain  pan    vp    
            // 1996 152   10.0  22.5  10.0   0.0   2.6  14.0
            Assert.AreEqual(weather.Radn, 10, delta);
            Assert.AreEqual(weather.MaxT, 22.5, delta);
            Assert.AreEqual(weather.MinT, 10.0, delta);
            Assert.AreEqual(weather.Rain, 0.0, delta);
            Assert.AreEqual(weather.VP, 14.0, delta);

            // ################## YEAR 2 - leap year
            // 1st June 1952 in weather file
            // year day  radn  maxt   mint  rain  pan    vp    
            // 1952 153   16.0  20.6   6.1   0.0   2.8   9.7
            Utilities.CallEvent(weather, "DoWeather");
            Assert.AreEqual(weather.Radn, 16, delta);
            Assert.AreEqual(weather.MaxT, 20.6, delta);
            Assert.AreEqual(weather.MinT, 6.1, delta);
            Assert.AreEqual(weather.Rain, 0.0, delta);
            Assert.AreEqual(weather.VP, 9.7, delta);

            // Advance 365 days
            for (int i = 0; i < 364; i++)
                Utilities.CallEvent(weather, "DoWeather");

            // 31st May 1953 in weather file
            // year day  radn  maxt   mint  rain  pan    vp    
            // 1953 151   14.0  23.3   5.6   0.0   2.8  10.1
            Assert.AreEqual(weather.Radn, 14, delta);
            Assert.AreEqual(weather.MaxT, 23.3, delta);
            Assert.AreEqual(weather.MinT, 5.6, delta);
            Assert.AreEqual(weather.Rain, 0.0, delta);
            Assert.AreEqual(weather.VP, 10.1, delta);

            // ################## YEAR 3 - year after leap year
            // 1st June 1993 in weather file
            // year day   radn  maxt   mint  rain  pan    vp    
            // 1993 152    9.0  24.0  14.0   0.0   2.6  16.0
            Utilities.CallEvent(weather, "DoWeather");
            Assert.AreEqual(weather.Radn, 9, delta);
            Assert.AreEqual(weather.MaxT, 24.0, delta);
            Assert.AreEqual(weather.MinT, 14.0, delta);
            Assert.AreEqual(weather.Rain, 0.0, delta);
            Assert.AreEqual(weather.VP, 16.0, delta);

            // Advance 363 days
            for (int i = 0; i < 363; i++)
                Utilities.CallEvent(weather, "DoWeather");

            // 31st May 1994 in weather file
            // year day   radn  maxt   mint  rain  pan    vp    
            // 1994 151   13.0  23.0  13.0   4.2   0.8  17.0
            Utilities.CallEvent(weather, "DoWeather");
            Assert.AreEqual(weather.Radn, 13, delta);
            Assert.AreEqual(weather.MaxT, 23.0, delta);
            Assert.AreEqual(weather.MinT, 13.0, delta);
            Assert.AreEqual(weather.Rain, 4.2, delta);
            Assert.AreEqual(weather.VP, 17.0, delta);
        }
    }
}
