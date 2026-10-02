using System;
using System.IO;
using APSIM.Core;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Phen;
using NUnit.Framework;

namespace UnitTests.Functions
{
    [TestFixture]
    public class WheatColdVrnResponseTests
    {
        // Middle-day regression values from the existing APSIMX Wheat model with
        // yesterday=today=tomorrow Tmin/Tmax (removes 3-day edge effects).
        [TestCase(-5.0, 0.0, 0)]
        [TestCase(0.0, 5.0, 0.7183671085246102)]
        [TestCase(5.0, 10.0, 0.3070408288042498)]
        [TestCase(10.0, 15.0, 0.13123383494884905)]
        public void ColdVrnResponse_FromWheatModel_ReturnsExpectedMiddleDayValue_WithThreeIdenticalDays(double minT, double maxT, double expected)
        {
            var weather = new FixedWeather
            {
                MinT = minT,
                MaxT = maxT,
                YesterdaysMetData = new DailyMetDataFromFile { MaxT = maxT, MinT = minT },
                TomorrowsMetData = new DailyMetDataFromFile { MaxT = maxT, MinT = minT }
            };

            var model = BuildModelFromWheatResource(weather);

            Utilities.CallEvent(model, "DailyInitialisation", null);

            TestContext.Progress.WriteLine($"ColdVrnResponse debug for Tmin={minT}, Tmax={maxT}");
            for (int hour = 0; hour < model.SubDailyInput.Count; hour++)
            {
                TestContext.Progress.WriteLine(
                    $"Hour {hour:00}: T={model.SubDailyInput[hour]:F6}, Response={model.SubDailyResponse[hour]:F6}");
            }
            // string csvPath = WriteHourlyDebugCsv(minT, maxT, model);
            // TestContext.Progress.WriteLine($"CSV written: {csvPath}");

            double actual = model.Value();

            Assert.That(actual, Is.EqualTo(expected).Within(1e-10));
        }

        [Test]
        public void ColdVrnResponse_FromWheatModel_UsesHourlySinPpAdjustedMethod()
        {
            var weather = new FixedWeather
            {
                MinT = 2.0,
                MaxT = 8.0,
                YesterdaysMetData = new DailyMetDataFromFile { MaxT = 8.0, MinT = 2.0 },
                TomorrowsMetData = new DailyMetDataFromFile { MaxT = 8.0, MinT = 2.0 }
            };

            var model = BuildModelFromWheatResource(weather);
            var interpolation = model.Node.FindChild<IInterpolationMethod>("InterpolationMethod", recurse: true);

            Assert.That(interpolation, Is.TypeOf<HourlySinPpAdjusted>());
        }

        private static SubDailyInterpolation BuildModelFromWheatResource(FixedWeather weather)
        {
            Plant wheat = Utilities.GetModelFromResource<Plant>("Wheat");
            Node.Create(wheat);
            Utilities.ResolveLinks(wheat);

            SubDailyInterpolation model = wheat.Node.FindChild<SubDailyInterpolation>("ColdVrnResponse", recurse: true);
            HourlySinPpAdjusted interpolationMethod = model.Node.FindChild<HourlySinPpAdjusted>("InterpolationMethod", recurse: true);
            Models.PMF.Phen.ColdVrnResponse response = model.Node.FindChild<Models.PMF.Phen.ColdVrnResponse>("Response", recurse: true);

            Utilities.InjectLink(model, "MetData", weather);
            Utilities.InjectLink(interpolationMethod, "MetData", weather);
            Utilities.InjectLink(response, "camp", new CAMP { Params = new CultivarRateParams() });

            return model;
        }

        private static string WriteHourlyDebugCsv(double minT, double maxT, SubDailyInterpolation model)
        {
            string folder = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ColdVrnResponseDebug");
            Directory.CreateDirectory(folder);
            string fileName = $"coldvrn_tmin_{minT:0.##}_tmax_{maxT:0.##}.csv";
            string filePath = Path.Combine(folder, fileName);
            double modelValue = model.Value();

            using (var writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine("Hour,Temperature,Response,MinT,MaxT,ModelValue");
                for (int hour = 0; hour < model.SubDailyInput.Count; hour++)
                {
                    writer.WriteLine($"{hour},{model.SubDailyInput[hour]:F6},{model.SubDailyResponse[hour]:F6},{minT:F6},{maxT:F6},{modelValue:F10}");
                }
            }

            return filePath;
        }

        [Serializable]
        private class FixedWeather : Model, IWeather
        {
            public DateTime StartDate { get; set; } = new DateTime(2000, 1, 1);
            public DateTime EndDate { get; set; } = new DateTime(2000, 12, 31);
            public double MaxT { get; set; }
            public double MinT { get; set; }
            public double MeanT => (MaxT + MinT) / 2.0;
            public double VPD => 0.0;
            public double Rain { get; set; }
            public double PanEvap { get; set; }
            public double Radn { get; set; }
            public double VP { get; set; }
            public double Wind { get; set; }
            public double CO2 { get; set; }
            public double AirPressure { get; set; }
            public double DiffuseFraction { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Tav { get; set; }
            public double Amp { get; set; }
            public string FileName { get; set; }

            public DailyMetDataFromFile TomorrowsMetData { get; set; }

            public DailyMetDataFromFile YesterdaysMetData { get; set; }

            public double CalculateDayLength(double twilight)
            {
                return 12.0;
            }

            public double CalculateSunRise()
            {
                return 6.0;
            }

            public double CalculateSunSet()
            {
                return 18.0;
            }
        }
    }
}