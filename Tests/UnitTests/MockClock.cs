

namespace UnitTests
{
    using Models;
    using System;

    class MockClock : IClock
    {
        public DateTime Today { get; set; }
    }
}
