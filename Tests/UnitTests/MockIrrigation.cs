

namespace UnitTests
{
    using Models;
    using Models.Soils;
    using System;

    class MockIrrigation : IIrrigation
    {
        public double IrrigationApplied { get; set; }

        public bool WillRunoff  { get; set; }

        public double Depth { get; set; }

        public event EventHandler<IrrigationApplicationType> Irrigated;

        public void Apply(double amount, double depth = 0, double efficiency = 1, bool willRunoff = false)
        {
            Irrigated.Invoke(this, new IrrigationApplicationType());
        }
    }
}
