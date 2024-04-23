

namespace UnitTests
{
    using Models;
    using Models.Core;
    using System;

    [Serializable]
    class MockClock : Model, IClock
    {
        public DateTime Today { get; set; }
        public int NumberOfTicks { get; set; }
        public double FractionComplete { get { return 1.0; } }
        public DateTime StartDate { get; set; }
       
        public DateTime EndDate { get; set; }
    }
}
