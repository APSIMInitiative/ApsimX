

namespace UnitTests
{
    using Models;
    using System;

    [Serializable]
    class MockClock : IClock
    {
        public DateTime Today { get; set; }
        public double FractionComplete { get { return 1.0; } }
    }
}
