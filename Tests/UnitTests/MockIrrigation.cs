

namespace UnitTests
{
    using Models;
    using Models.Core;
    using Models.Soils;
    using System;

    [Serializable]
    class MockIrrigation : Model, IIrrigation
    {
        public double IrrigationApplied { get; set; }

        public double Depth { get; set; }

        public double Duration { get; set; }

        public double Efficiency { get; set; }

        public bool WillRunoff { get; set; }

        public event EventHandler<IrrigationApplicationType> Irrigated;

        public void Apply(double amount, double depth = 0.0, string time = "00:00", double duration = 1.0, double efficiency = 1.0, bool willRunoff = false, double no3 = -1, double nh4 = -1, bool doOutput = true)
        {
            Irrigated.Invoke(this, new IrrigationApplicationType());
        }
    }
}
