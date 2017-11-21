

namespace UnitTests
{
    using Models;
    using Models.Soils;
    using System;

    [Serializable]
    class MockIrrigation : IIrrigation
    {
        public double IrrigationApplied { get; set; }

        public double Depth { get; set; }

        public double StartTime { get; set; }

        public double Duration { get; set; }

        public double Efficiency { get; set; }

        public bool WillIntercept { get; set; }

        public bool WillRunoff { get; set; }

        public event EventHandler<IrrigationApplicationType> Irrigated;

        public void Apply(double amount, double depth = 0.0, double startTime = 0.0, double duration = 1.0, double efficiency = 1.0, bool willIntercept = false, bool willRunoff = false)
        {
            Irrigated.Invoke(this, new IrrigationApplicationType());
        }
    }
}
