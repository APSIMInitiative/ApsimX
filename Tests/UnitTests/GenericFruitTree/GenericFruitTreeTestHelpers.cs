using System;
using System.Collections.Generic;
using APSIM.Core;
using Models;
using Models.Core;
using Models.Interfaces;

namespace UnitTests.Agroforestry
{
    internal class TestClock : IClock
    {
        public DateTime Today { get; set; }
        public double FractionComplete { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    internal class TestWeather : IWeather
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public double MaxT { get; set; }
        public double MinT { get; set; }
        public double MeanT => (MaxT + MinT) / 2.0;
        public double VPD { get; set; }
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

    internal class TestSummary : ISummary
    {
        public List<(IModel Model, string Message, MessageType MessageType)> Messages { get; } = new();

        public void WriteMessage(IModel model, string message, MessageType messageType)
        {
            Messages.Add((model, message, messageType));
        }

        public void WriteMessagesToDataStore()
        {
        }
    }

    internal class ConstantFunction : Model, IFunction
    {
        public ConstantFunction(double value)
        {
            ReturnValue = value;
        }

        public double ReturnValue { get; set; }

        public virtual double Value(int arrayIndex = -1)
        {
            return ReturnValue;
        }
    }

    internal class RecordingFunction : ConstantFunction
    {
        public RecordingFunction(double value)
            : base(value)
        {
        }

        public int LastArrayIndex { get; private set; } = -1;

        public override double Value(int arrayIndex = -1)
        {
            LastArrayIndex = arrayIndex;
            return base.Value(arrayIndex);
        }
    }
}
