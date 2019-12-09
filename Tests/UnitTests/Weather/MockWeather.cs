namespace UnitTests.Weather
{
    using Models.Core;
    using Models.Interfaces;
    using System;

    [Serializable]
    class MockWeather : Model, IWeather
    {
        public string FileName { get; set; }

        public double Amp { get; set; }

        public double CO2 { get; set; }

        public double AirPressure { get; set; }

        public DateTime EndDate { get; set; }

        public double Latitude { get; set; }

        public double MaxT { get; set; }

        public double MinT { get; set; }

        public double MeanT { get; set; }

        public double Radn { get; set; }

        public double Rain { get; set; }

        public DateTime StartDate { get; set; }

        public double Tav { get; set; }

        public double VP { get; set; }

        public double VPD { get; set; }

        public double Wind { get; set; }

        public double CalculateDayLength(double Twilight)
        {
            throw new NotImplementedException();
        }
    }
}
