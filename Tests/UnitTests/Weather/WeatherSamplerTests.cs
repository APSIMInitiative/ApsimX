namespace UnitTests.Weather
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using Models;
    using Models.Climate;
    using Models.Core;
    using NUnit.Framework;

    /// <summary>Tests for weather randomiser</summary>
    class WeatherSamplerTests
    {
        /// <summary>Tests a 3 year weather selection.</summary>
        [Test]
        public void ThreeYearTest()
        {
            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            const double delta = 0.000001;

            var weatherFilePath = Path.GetFullPath(Path.Combine(binDirectory, "..", "..", "..", "Examples", "WeatherFiles", "AU_Dalby.met"));

            var baseSim = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock() 
                    {
                        StartDate = new DateTime(3000, 6, 1)
                    },
                    new WeatherSampler()
                    {
                        FileName = weatherFilePath,
                        Years = new int[] { 1995, 1952, 1993 }
                    },
                    new MockSummary()
                }
            };
            var clock = baseSim.Children[0] as Clock;
            var weatherSampler = baseSim.Children[1];

            Utilities.CallEvent(clock, "SimulationCommencing");
            Utilities.InjectLink(weatherSampler, "simulation", baseSim);
            Utilities.InjectLink(weatherSampler, "clock", clock);

            var weather = baseSim.Children[1] as WeatherSampler;
            Utilities.CallEvent(weather, "StartOfSimulation");

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
            for (int i = 0; i < 364; i++)
                AdvanceOneDay(weather, clock);

            // 31st May 1952 in weather file
            // year day  radn  maxt   mint  rain  pan    vp    
            // 1952 152   16.0  21.7   6.1   0.0   2.8   9.4
            Assert.AreEqual(weather.Radn, 16, delta);
            Assert.AreEqual(weather.MaxT, 21.7, delta);
            Assert.AreEqual(weather.MinT, 6.1, delta);
            Assert.AreEqual(weather.Rain, 0.0, delta);
            Assert.AreEqual(weather.VP, 9.4, delta);

            // ################## YEAR 2 - leap year
            // 1st June 1952 in weather file
            // year day  radn  maxt   mint  rain  pan    vp    
            // 1952 153   16.0  20.6   6.1   0.0   2.8   9.7
            AdvanceOneDay(weather, clock);
            Assert.AreEqual(weather.Radn, 16, delta);
            Assert.AreEqual(weather.MaxT, 20.6, delta);
            Assert.AreEqual(weather.MinT, 6.1, delta);
            Assert.AreEqual(weather.Rain, 0.0, delta);
            Assert.AreEqual(weather.VP, 9.7, delta);

            // Advance 364 days
            for (int i = 0; i < 364; i++)
                AdvanceOneDay(weather, clock);

            // 31st May 1993 in weather file
            // year day  radn  maxt   mint  rain  pan    vp    
            // 1993 151   14.0  24.5   9.5   0.0   2.6  15.0
            Assert.AreEqual(weather.Radn, 14, delta);
            Assert.AreEqual(weather.MaxT, 24.5, delta);
            Assert.AreEqual(weather.MinT, 9.5, delta);
            Assert.AreEqual(weather.Rain, 0.0, delta);
            Assert.AreEqual(weather.VP, 15.0, delta);

            // ################## YEAR 3 - year after leap year
            // 1st June 1993 in weather file
            // year day   radn  maxt   mint  rain  pan    vp    
            // 1993 152    9.0  24.0  14.0   0.0   2.6  16.0
            AdvanceOneDay(weather, clock);
            Assert.AreEqual(weather.Radn, 9, delta);
            Assert.AreEqual(weather.MaxT, 24.0, delta);
            Assert.AreEqual(weather.MinT, 14.0, delta);
            Assert.AreEqual(weather.Rain, 0.0, delta);
            Assert.AreEqual(weather.VP, 16.0, delta);

            // Advance 365 days
            for (int i = 0; i < 365; i++)
                AdvanceOneDay(weather, clock);

            // ***** BACK TO FIRST YEAR (1995)
            // 1st Jun 1995 in weather file
            // year day  radn  maxt   mint  rain  pan    vp    
            // 1995 152   14.0  22.5  10.0   4.3   2.2  15.0
            Assert.AreEqual(weather.Radn, 14, delta);
            Assert.AreEqual(weather.MaxT, 22.5, delta);
            Assert.AreEqual(weather.MinT, 10.0, delta);
            Assert.AreEqual(weather.Rain, 4.3, delta);
            Assert.AreEqual(weather.VP, 15.0, delta);
        }
        [Test]
        public void WaterYearTest()
        {
            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            const double delta = 0.000001;

            var weatherFilePath = Path.GetFullPath(Path.Combine(binDirectory, "..", "..", "..", "Examples", "WeatherFiles", "AU_Dalby.met"));

            var baseSim = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock() 
                    {
                        StartDate = new DateTime(3000, 6, 1)
                    },
                    new WeatherSampler()
                    {
                        FileName = weatherFilePath,
                        Years = new int[] { 1995, 1952, 1993 },
                        SplitDate = "2-jun"
                    },
                    new MockSummary()
                }
            };
            var clock = baseSim.Children[0] as Clock;
            var weatherSampler = baseSim.Children[1];

            Utilities.CallEvent(clock, "SimulationCommencing");
            Utilities.InjectLink(weatherSampler, "simulation", baseSim);
            Utilities.InjectLink(weatherSampler, "clock", clock);

            var weather = baseSim.Children[1] as WeatherSampler;
            Utilities.CallEvent(weather, "StartOfSimulation");

            // ################## YEAR 1 
            // 1st Jun 1995 in weather file
            // year day  radn  maxt   mint  rain  pan    vp    
            // 1995 152   14.0  22.5  10.0   4.3   2.2  15.0
            Utilities.CallEvent(weather, "DoWeather");
            Assert.AreEqual(weather.Radn, 14, delta);
            Assert.AreEqual(weather.MaxT, 22.5, delta);
            Assert.AreEqual(weather.MinT, 10.0, delta);
            Assert.AreEqual(weather.Rain, 4.3, delta);
            Assert.AreEqual(weather.VP, 15.0, delta);

            // 2nd june will advance to next year
            AdvanceOneDay(weather, clock);
            // this is a leap year, and the day number will jump by 2 (to 154)
            // 1952 153   16.0  20.6   6.1   0.0   2.8   9.7 300070
            // 1952 154   16.0  20.6   7.2   0.0   2.8   7.5 300070
            Assert.AreEqual(weather.Radn, 16, delta);
            Assert.AreEqual(weather.MaxT, 20.6, delta);
            Assert.AreEqual(weather.MinT, 7.2, delta);
            Assert.AreEqual(weather.Rain, 0.0, delta);
            Assert.AreEqual(weather.VP, 7.5, delta);
        }
        private void AdvanceOneDay(IModel weather, Clock clock)
        {
            DateTime d = clock.Today.AddDays(1);
            Utilities.SetProperty(clock, "Today", d);
            Utilities.CallEvent(weather, "DoWeather");
        }
    }
}
