using System;
using Models.Core;
using Models.Functions.SupplyFunctions;
using Models.Interfaces;
using NUnit.Framework;

namespace UnitTests.Functions
{
    [TestFixture]
    public class RUECO2FunctionTests
    {
        [Test]
        public void RUECO2Function_C3_DirectCall_WorksAcrossTemperatureAndCO2Grid()
        {
            double[] meanTemperatures = { -5, 0, 5, 10, 15, 20, 25, 30 };
            double[] co2Levels = { 300, 350, 400, 450, 500, 550, 600, 650, 700 };

            foreach (double meanT in meanTemperatures)
            {
                foreach (double co2 in co2Levels)
                {
                    var weather = new FixedWeather
                    {
                        MinT = meanT,
                        MaxT = meanT,
                        CO2 = co2
                    };

                    var function = new RUECO2Function
                    {
                        PhotosyntheticPathway = "C3"
                    };

                    Utilities.InjectLink(function, "MetData", weather);

                    double actual = function.Value();
                    double expected = CalculateExpectedC3(meanT, co2);

                    Assert.That(
                        actual,
                        Is.EqualTo(expected).Within(1e-12),
                        $"Unexpected C3 RUE CO2 factor for meanT={meanT}, CO2={co2}");
                }
            }
        }

        [Test]
        public void RUECO2Function_C3_DirectCall_ThrowsWhenMeanTemperatureAbove46Point5()
        {
            var weather = new FixedWeather
            {
                MinT = 47.0,
                MaxT = 47.0,
                CO2 = 700.0
            };

            var function = new RUECO2Function
            {
                PhotosyntheticPathway = "C3"
            };

            Utilities.InjectLink(function, "MetData", weather);

            Exception ex = Assert.Throws<Exception>(() => function.Value());
            Assert.That(ex.Message, Is.EqualTo("Average daily temperature too high for RUE CO2 Function"));
        }

        private static double CalculateExpectedC3(double meanT, double co2)
        {
            double compensationPoint = (163.0 - meanT) / (5.0 - 0.1 * meanT);
            if (co2 == 350.0)
                return 1.0;

            return ((co2 - compensationPoint) * (350.0 + 2.0 * compensationPoint))
                   / ((co2 + 2.0 * compensationPoint) * (350.0 - compensationPoint));
        }

        [Test]
        public void RUECO2Function_C4_DirectCall_WorksAcrossTemperatureAndCO2Grid()
        {
            double[] meanTemperatures = { -5, 0, 5, 10, 15, 20, 25, 30 };
            double[] co2Levels = { 300, 350, 400, 450, 500, 550, 600, 650, 700 };

            foreach (double co2 in co2Levels)
            {
                double? firstValueAtThisCo2 = null;

                foreach (double meanT in meanTemperatures)
                {
                    var weather = new FixedWeather
                    {
                        MinT = meanT,
                        MaxT = meanT,
                        CO2 = co2
                    };

                    var function = new RUECO2Function
                    {
                        PhotosyntheticPathway = "C4"
                    };

                    Utilities.InjectLink(function, "MetData", weather);

                    double actual = function.Value();
                    double expected = 0.000143 * co2 + 0.95;

                    Assert.That(
                        actual,
                        Is.EqualTo(expected).Within(1e-12),
                        $"Unexpected C4 RUE CO2 factor for meanT={meanT}, CO2={co2}");

                    if (firstValueAtThisCo2.HasValue)
                    {
                        Assert.That(
                            actual,
                            Is.EqualTo(firstValueAtThisCo2.Value).Within(1e-12),
                            $"Expected C4 RUE CO2 factor to be independent of temperature for CO2={co2}");
                    }

                    firstValueAtThisCo2 = actual;
                }
            }
        }

        [Test]
        public void RUECO2Function_DirectCall_ThrowsWhenPhotosyntheticPathwayIsUnknown()
        {
            var weather = new FixedWeather
            {
                MinT = 20.0,
                MaxT = 20.0,
                CO2 = 400.0
            };

            var function = new RUECO2Function
            {
                PhotosyntheticPathway = "CAM"
            };

            Utilities.InjectLink(function, "MetData", weather);

            Exception ex = Assert.Throws<Exception>(() => function.Value());
            Assert.That(ex.Message, Is.EqualTo("Unknown photosynthetic pathway in RUECO2Function"));
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
