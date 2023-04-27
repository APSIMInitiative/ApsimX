using Models;
using Models.Core;
using System;

namespace UnitTests
{

    [Serializable]
    class MockClock : Model, IClock
    {
        public DateTime Today { get; set; }
        public int NumberOfTicks { get; set; }
        public double FractionComplete { get { return 1.0; } }
    }
}
